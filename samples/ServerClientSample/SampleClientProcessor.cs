﻿using System;
using System.Text;
using System.Threading.Tasks;
using NetX;

namespace ServerClientSample
{
    public class SampleClientProcessor : INetXClientProcessor
    {
        public async Task OnConnectedAsync(INetXClientSession client)
        {
            Console.WriteLine($"[CLIENT] Connected");

            await client.SendAsync(Encoding.UTF8.GetBytes("Requisicao 1"));

            //var response = Encoding.UTF8.GetString(responseBytes);
        }

        public Task OnDisconnectedAsync()
        {
            Console.WriteLine($"[CLIENT] Disconnected");

            return Task.CompletedTask;
        }

        public async Task OnReceivedMessageAsync(INetXClientSession client, NetXMessage message)
        {
            Console.WriteLine($"[CLIENT] Received MessageId = {message.Id}, Length = {message.Buffer.Count}");

            var messageId = message.Id;
            var clientResponseBytes = Encoding.UTF8.GetBytes("Resposta final");

            await client.ReplyAsync(messageId, clientResponseBytes);
        }

        public int GetReceiveMessageSize(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public void ProcessReceivedBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[CLIENT] Processing received buffer");
        }

        public void ProcessSendBuffer(INetXClientSession client, in ArraySegment<byte> buffer)
        {
            Console.WriteLine($"[CLIENT] Processing send buffer");
        }
    }
}
