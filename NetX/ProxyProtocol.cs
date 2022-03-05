using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetX
{
    /// <summary>
    /// Implements proxy protocol v2 https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt
    /// Which can be used to read the original source IP and port where the service is behind a load
    /// balancer implementing proxy protocol v2 i.e. HAProxy or AWS NLB
    /// </summary>
    public class ProxyProtocol
    {
        private const int SignatureLength = 16;
        private const int Ipv4Length = 4;
        private const int Ipv6Length = 16;

        private static readonly byte[] proxyProtocolV2Signature = { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };

        private readonly IPEndPoint remoteEndPoint;
        private readonly Stream stream;
        private byte[] readBuffer;
        private int bytesRead;

        public ProxyProtocol(Stream stream, IPEndPoint remoteEndPoint)
        {
            this.stream = stream;
            this.remoteEndPoint = remoteEndPoint;
            readBuffer = new byte[SignatureLength];
        }

        public async Task<IPEndPoint> GetRemoteEndpoint()
        {
            if (!await IsProxyProtocolV2() || await IsLocalConnection())
            {
                return remoteEndPoint;
            }

            if (GetAddressFamily(readBuffer) == AddressFamily.InterNetwork)
            {
                return new IPEndPoint(GetSourceAddressIpv4(readBuffer), GetSourcePortIpv4(readBuffer));
            }

            if (GetAddressFamily(readBuffer) == AddressFamily.InterNetworkV6)
            {
                return new IPEndPoint(GetSourceAddressIpv6(readBuffer), GetSourcePortIpv6(readBuffer));
            }

            throw new NotImplementedException("Address family of connection not supported");
        }

        public async Task<bool> IsLocalConnection() => await IsProxyProtocolV2()
                                                       && GetCommand(readBuffer) == "LOCAL";

        public async Task<bool> IsProxyProtocolV2()
        {
            await GetProxyProtocolHeader();
            return HasProxyProtocolSignature(readBuffer) && IsProtocolV2(readBuffer);
        }

        private async Task GetProxyProtocolHeader()
        {
            if (bytesRead > 0)
            {
                return;
            }

            await ReadNextBytesInToBuffer(readBuffer.Length);

            Array.Resize(ref readBuffer, bytesRead + readBuffer[SignatureLength - 1]);

            await ReadNextBytesInToBuffer(readBuffer[SignatureLength - 1]);
        }

        internal async Task<byte[]> GetBytesWithoutProxyHeader() =>
            readBuffer.Skip(await GetProxyHeaderLength()).ToArray();

        private async Task<int> GetProxyHeaderLength() =>
            await IsProxyProtocolV2() ? SignatureLength + GetLength(readBuffer) : 0;

        internal async Task<int> GetLengthWithoutProxyHeader() => bytesRead - await GetProxyHeaderLength();

        private async Task ReadNextBytesInToBuffer(int length)
        {
            if (bytesRead + length > readBuffer.Length)
            {
                Array.Resize(ref readBuffer, bytesRead + length);
            }

            bytesRead += await stream.ReadAsync(readBuffer, bytesRead, length);
        }

        private static string GetCommand(byte[] header)
        {
            var version = header[12];
            return (version & 0x0F) == 0x01 ? "PROXY" : "LOCAL";
        }

        private static bool IsProtocolV2(byte[] header)
        {
            var version = header[12];
            return (version & 0xF0) == 0x20;
        }

        private static AddressFamily GetAddressFamily(byte[] header)
        {
            var family = header[13] & 0xF0;
            switch (family)
            {
                case 0x00:
                    return AddressFamily.Unspecified;
                case 0x10:
                    return AddressFamily.InterNetwork;
                case 0x20:
                    return AddressFamily.InterNetworkV6;
                case 0x30:
                    return AddressFamily.Unix;
                default:
                    throw new ArgumentException("Invalid address family");
            }
        }

        private static bool HasProxyProtocolSignature(byte[] signatureBytes) =>
            signatureBytes.Length >= proxyProtocolV2Signature.Length &&
            signatureBytes.Take(proxyProtocolV2Signature.Length).SequenceEqual(proxyProtocolV2Signature);

        private static int GetLength(byte[] header) =>
            BytesToUInt16(header.Skip(SignatureLength - 2).Take(2).ToArray());

        private static IPAddress GetSourceAddressIpv4(byte[] header) =>
            new IPAddress(header.Skip(SignatureLength).Take(Ipv4Length).ToArray());

        internal static IPAddress GetDestinationAddressIpv4(byte[] header) =>
            new IPAddress(header.Skip(SignatureLength + Ipv4Length).Take(Ipv4Length).ToArray());

        private static int GetSourcePortIpv4(byte[] header) =>
            BytesToUInt16(header.Skip(SignatureLength + 2 * Ipv4Length).Take(2).ToArray());

        internal static int GetDestinationPortIpv4(byte[] header) =>
            BytesToUInt16(header.Skip(SignatureLength + 2 * Ipv4Length + 2).Take(2).ToArray());

        private static IPAddress GetSourceAddressIpv6(byte[] header) =>
            new IPAddress(header.Skip(SignatureLength).Take(Ipv6Length).ToArray());

        internal static IPAddress GetDestinationAddressIpv6(byte[] header) =>
            new IPAddress(header.Skip(SignatureLength + Ipv6Length).Take(Ipv6Length).ToArray());

        private static int GetSourcePortIpv6(byte[] header) =>
            BytesToUInt16(header.Skip(SignatureLength + 2 * Ipv6Length).Take(2).ToArray());

        internal static int GetDestinationPortIpv6(byte[] header) =>
            BytesToUInt16(header.Skip(SignatureLength + 2 * Ipv6Length + 2).Take(2).ToArray());

        private static int BytesToUInt16(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToUInt16(bytes, 0);
        }
    }
}
