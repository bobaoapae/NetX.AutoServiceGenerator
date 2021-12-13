public async {2} {1}({4})
    {{
        var {0}_{1}_stream = _streamManager.GetStream("{0}_{1}", 4096, true);
        try
        {{
            {0}_{1}_stream.ExWrite("{0}");
            {0}_{1}_stream.ExWrite(Convert.ToUInt16({7}));
            
{5}

            await _session.Session.RequestAsync({0}_{1}_stream);

{6}

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