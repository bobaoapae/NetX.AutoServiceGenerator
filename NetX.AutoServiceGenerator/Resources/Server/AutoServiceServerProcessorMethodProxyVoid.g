private async ValueTask InternalProxy_{0}_{1}_{2}_{3}({4}Session session, NetXMessage message, int offset)
    {{
        var inputBuffer = message.Buffer;

{6}

        _currentSession.Value = session;
        await _{5}.{3}({7});
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        try
        {{
            stream.ExWrite((byte)0);
            {8}
            
            await session.Session.ReplyAsync(message.Id, stream);
        }}
        catch (Exception)
        {{
            throw;
        }}
        finally
        {{
            await stream.DisposeAsync();
        }}
    }}