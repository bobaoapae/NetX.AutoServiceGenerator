 public async {2} {1}({4})
    {{
        var {0}_{1}_stream = _streamManager.GetStream("{0}_{1}", 4096, true);
        try
        {{
            {0}_{1}_stream.ExWrite("{0}");
            {0}_{1}_stream.ExWrite(Convert.ToUInt16({6}));
            
            {5}

            var {0}_{1}_Buffer_Result = await _client.RequestAsync({0}_{1}_stream);
            var {0}_{1}_Buffer_Result_Offset = {0}_{1}_Buffer_Result.Offset;
            
            {0}_{1}_Buffer_Result.Read(ref {0}_{1}_Buffer_Result_Offset, out {3} {0}_{1}_Result);

            return {0}_{1}_Result;

        }}
        catch (Exception)
        {{
            throw;
        }}
        finally
        {{
            await {0}_{1}_stream.DisposeAsync();
        }}
    }}