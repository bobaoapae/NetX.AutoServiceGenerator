private async ValueTask InternalProxy_{0}_{1}_{2}_{3}({4}Session session, NetXMessage message, int offset)
    {{
        var inputBuffer = message.Buffer;
        _currentSession.Value = session;
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        stream.Advance(2);
        try
        {{
{6}

            await _{5}.{3}({7});
{8}
            
            stream.Position = 0;
            stream.ExWrite((short)200);
            await session.Session.ReplyAsync(message.Id, stream);
        }}
        catch (Exception ex)
        {{
            _logger?.LogError(ex, "{{identity}}: Unexpected error processing request to ({{serviceName}}):({{methodName}}) from session session({{sessionId}}) ", _identity, "{0}", "{3}", session.Session.Id);
            stream.Position = 0;
            stream.ExWrite((short)500);
            await session.Session.ReplyAsync(message.Id, stream);
            throw;
        }}
        finally
        {{
            await stream.DisposeAsync();
        }}
    }}