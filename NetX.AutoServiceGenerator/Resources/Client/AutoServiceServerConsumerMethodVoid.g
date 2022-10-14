public async {2} {1}({4})
    {{
        var {0}_{1}_stream = _streamManager.GetStream("{0}_{1}", 4096, true);
        try
        {{
            {0}_{1}_stream.ExWrite("{0}".Length);
            {0}_{1}_stream.ExWrite("{0}");
            {0}_{1}_stream.ExWrite(Convert.ToUInt16({7}));
            
{5}

            var {0}_{1}_Buffer_Result = await _client.RequestAsync({0}_{1}_stream);
            var {0}_{1}_Buffer_Result_Offset = {0}_{1}_Buffer_Result.Offset;

            {0}_{1}_Buffer_Result.Read(ref {0}_{1}_Buffer_Result_Offset, out short {0}_{1}_Buffer_Result_Response_Code);

            if ({0}_{1}_Buffer_Result_Response_Code != 200)
            {{
                switch({0}_{1}_Buffer_Result_Response_Code)
                {{
                    case 500: 
                    {{
                        throw new Exception("Unexpected error ocurred on server side");
                    }}
                    case 501: 
                    {{
                        {0}_{1}_Buffer_Result.Read(ref {0}_{1}_Buffer_Result_Offset, out bool {0}_{1}_Buffer_Result_Response_Code_Complement);
                        throw new NotImplementedException({0}_{1}_Buffer_Result_Response_Code_Complement ? "Service not implemented" : "Method not supported");
                    }}
                    default: 
                    {{
                        throw new Exception("Unexpected response code: " + {0}_{1}_Buffer_Result_Response_Code);
                    }}
                }}
            }}
            
{6}

        }}
        finally
        {{
            await {0}_{1}_stream.DisposeAsync();
        }}
    }}