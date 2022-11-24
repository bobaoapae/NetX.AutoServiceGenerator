private async ValueTask InternalProxy_{0}_{1}_{2}_{3}(INetXClientSession client, NetXMessage {0}_{1}_message, int {0}_{1}_offset)
    {{
        var {0}_{1}_inputBuffer = {0}_{1}_message.Buffer;
        
        var {0}_{1}_stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        {0}_{1}_stream.Advance(2);

        try
        {{
{5}

            var {0}_{1}_{2}_{3}_Result = await _{4}.{3}({6});

{7}

            {0}_{1}_stream.Position = 0;
            {0}_{1}_stream.ExWrite((short)200);
            await client.ReplyAsync({0}_{1}_message.Id, {0}_{1}_stream);
        }}
        catch (Exception ex)
        {{
            _logger?.LogError(ex, "{{identity}}: Unexpected error processing request to ({{serviceName}}):({{methodName}})) ", _identity, "{0}", "{3}");
            {0}_{1}_stream.Position = 0;
            {0}_{1}_stream.ExWrite((short)500);
            await client.ReplyAsync({0}_{1}_message.Id, {0}_{1}_stream);
            throw;
        }}
        finally
        {{
            await {0}_{1}_stream.DisposeAsync();
        }}
    }}