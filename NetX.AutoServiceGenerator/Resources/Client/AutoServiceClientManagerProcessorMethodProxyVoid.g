private async ValueTask InternalProxy_{0}_{1}_{2}_{3}(INetXClientSession client, NetXMessage message, int offset)
    {{
        var inputBuffer = message.Buffer;

{5}
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        stream.Advance(2);
        try
        {{
            await _{4}.{3}({6});

{7}
            
            stream.Position = 0;
            stream.ExWrite((short)200);
            await client.ReplyAsync(message.Id, stream);
        }}
        catch (Exception ex)
        {{
            _logger?.LogError(ex, "{{identity}}: Unexpected error processing request to ({{serviceName}}):({{methodName}})) ", _identity, "{0}", "{3}");
            stream.Position = 0;
            stream.ExWrite((short)500);
            await client.ReplyAsync(message.Id, stream);
            throw;
        }}
        finally
        {{
            await stream.DisposeAsync();
        }}
    }}