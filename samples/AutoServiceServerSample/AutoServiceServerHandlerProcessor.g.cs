using NetX;
using System.Threading.Tasks;
using Microsoft.IO;
using NetX.AutoServiceGenerator.Definitions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using AutoServiceServerSample.Definitions;

namespace AutoServiceServerSample;

public class AutoServiceManagerProcessor : INetXServerProcessor
{
    public delegate bool TryGetCallingSession(out AutoServiceServerManagerSession session);
    public delegate bool TryGetSession(Guid guid, out AutoServiceServerManagerSession session);
    private delegate ValueTask InternalProxy(AutoServiceServerManagerSession session, NetXMessage message, int offset);

    private readonly ConcurrentDictionary<Guid, AutoServiceServerManagerSession> _sessions;
    private readonly Dictionary<ushort, Dictionary<ushort, InternalProxy>> _serviceProxys;

    private AsyncLocal<AutoServiceServerManagerSession> _currentSession;

    private AutoServiceServerManager _autoServiceServerManager;
    private RecyclableMemoryStreamManager _streamManager;

    public AutoServiceManagerProcessor(AutoServiceServerManager autoAutoServiceServerManager, RecyclableMemoryStreamManager streamManager)
    {
        _currentSession = new AsyncLocal<AutoServiceServerManagerSession>();
        _autoServiceServerManager = autoAutoServiceServerManager;
        _streamManager = streamManager;
        _serviceProxys = new Dictionary<ushort, Dictionary<ushort, InternalProxy>>();
        _sessions = new ConcurrentDictionary<Guid, AutoServiceServerManagerSession>();
        InitializeServices();
        LoadProxys();
    }

    private void InitializeServices()
    {
        _autoServiceServerSample = new AutoServiceServerSample(TryGetCallingSessionProxy, _sessions.TryGetValue);
    }

    private void LoadProxys()
    {
        if(!_serviceProxys.ContainsKey(0))
            _serviceProxys.Add(0, new Dictionary<ushort, InternalProxy>());
        
        if(!_serviceProxys[0].ContainsKey(0))
            _serviceProxys[0].Add(0, InternalProxy_AutoServiceSample_0_0_TryDoSomething);
    }
    
    
    private bool TryGetCallingSessionProxy(out AutoServiceServerManagerSession session)
    {
        if (_currentSession.Value != null)
        {
            session = _currentSession.Value;
            return true;
        }

        session = null;

        return false;
    }

    public Task OnSessionConnectAsync(INetXSession session)
    {
        if(!_sessions.TryAdd(session.Id, new AutoServiceServerManagerSession(session, _streamManager)))
            session.Disconnect();
        return Task.CompletedTask;
    }

    public Task OnSessionDisconnectAsync(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    public Task OnReceivedMessageAsync(INetXSession session, NetXMessage message)
    {
        var buffer = message.Buffer;
        var offset = buffer.Offset;
        
        buffer.Read(ref offset, out ushort interfaceCode);
        buffer.Read(ref offset, out ushort methodCode);

        Task.Run(async () =>
        {
            if(!_sessions.TryGetValue(session.Id, out var autoServiceSession))
                session.Disconnect();
            await _serviceProxys[interfaceCode][methodCode](autoServiceSession, message, offset);
        });
        
        return Task.CompletedTask;
    }

    public int GetReceiveMessageSize(INetXSession session, in ArraySegment<byte> buffer)
    {
        throw new NotImplementedException();
    }

    public void ProcessReceivedBuffer(INetXSession session, in ArraySegment<byte> buffer)
    {
        
    }

    public void ProcessSendBuffer(INetXSession session, in ArraySegment<byte> buffer)
    {
        
    }
    
    #region ServiceProviders

    private IAutoServiceServerSample _autoServiceServerSample;
    
    #endregion

    #region ServiceProxys

    private async ValueTask InternalProxy_AutoServiceSample_0_0_TryDoSomething(AutoServiceServerManagerSession session, NetXMessage message, int offset)
    {
        var inputBuffer = message.Buffer;
        inputBuffer.Read(ref offset, out string value);
        inputBuffer.Read(ref offset, out int value2);
        inputBuffer.Read(ref offset, out short value3);
        inputBuffer.Read(ref offset, out bool value5);
        _currentSession.Value = session;
        var autoServiceSample_0_0_TryDoSomething_Result = await _autoServiceServerSample.TryDoSomething(value, value2, value3, value5);
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("AutoServiceSample_0_0_TryDoSomething", 4096, true);
        try
        {
            stream.Write(autoServiceSample_0_0_TryDoSomething_Result);
            
            await session.Session.ReplyAsync(message.Id, stream);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    #endregion
}