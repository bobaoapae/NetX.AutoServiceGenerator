using AutoServiceServerSample.Definitions;
using Microsoft.IO;
using NetX;
using System;
using System.Net;
using AutoServiceClientSample.Definitions;

namespace AutoServiceServerSample;

public class AutoServiceServerManagerSession
{
    private RecyclableMemoryStreamManager _streamManager;
    public IAutoServiceClientSample AutoServiceClientSample { get; }
    
    public INetXSession Session { get; }

    public AutoServiceServerManagerSession(INetXSession session,  RecyclableMemoryStreamManager streamManager)
    {
        Session = session;
        _streamManager = streamManager;

        #region InitializeServices

        AutoServiceClientSample = new AutoServiceClientSampleConsumer(this, streamManager);

        #endregion
    }
    
}