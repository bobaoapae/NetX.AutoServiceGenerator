private async ValueTask InternalProxy_{0}_{1}_{2}_{3}(INetXClientSession client, NetXMessage message, int offset)
    {{
        var inputBuffer = message.Buffer;

{5}

        var {0}_{1}_{2}_{3}_Result = await _{4}.{3}({6});
        
        var stream = (RecyclableMemoryStream)_streamManager.GetStream("{0}_{1}_{2}_{3}", 4096, true);
        try
        {{
            stream.ExWrite({0}_{1}_{2}_{3}_Result);
            
            await client.ReplyAsync(message.Id, stream);
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