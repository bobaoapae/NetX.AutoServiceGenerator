private async ValueTask InternalProxy_{0}_{1}_{2}_{3}({4}Session session, NetXMessage {0}_{1}_message, int {0}_{1}_offset)
    {{
        if(!MemoryMarshal.TryGetArray({0}_{1}_message.Buffer, out var {0}_{1}_inputBuffer))
            return;

        _currentSession.Value = session;
        
        var {0}_{1}_stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        {0}_{1}_stream.Advance(2);
        try
        {{
{6}

            var {0}_{1}_{2}_{3}_Result = await _{5}.{3}({7});
{8}

            {0}_{1}_stream.Position = 0;
            {0}_{1}_stream.ExWrite((short)200);
            await session.Session.ReplyAsync({0}_{1}_message.Id, {0}_{1}_stream);
        }}
        catch (Exception ex)
        {{
            _logger?.LogError(ex, "{{identity}}: Unexpected error processing request to ({{serviceName}}):({{methodName}}) from session session({{sessionId}}) ", _identity, "{0}", "{3}", session.Session.Id);
            {0}_{1}_stream.Position = 0;
            {0}_{1}_stream.ExWrite((short)500);
            await session.Session.ReplyAsync({0}_{1}_message.Id, {0}_{1}_stream);
            throw;
        }}
        finally
        {{
            await {0}_{1}_stream.DisposeAsync();
        }}
    }}