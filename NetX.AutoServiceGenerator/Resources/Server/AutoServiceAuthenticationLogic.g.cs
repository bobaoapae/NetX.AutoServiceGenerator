        if(_pendingAuthentications.TryRemove(session.Id, out var taskCompletionSource))
        {{
            if(interfaceCode != "InternalIpcAuthentication" || methodCode != 1456)
            {{
                _logger?.LogError("{{identity}}: Received request but session ({{sessionId}}) was not propely authenticated", _identity, session.Id);
                session.Disconnect();
                return;
            }}
            _logger?.LogInformation("{{identity}}: Received authentication request from {{sessionId}}", _identity, session.Id);

            if(!MemoryMarshal.TryGetArray(message.Buffer, out var inputBuffer))
                return;
            inputBuffer.Read(ref offset, out {0} ipsInternalAuthProto);
            _currentSession.Value = autoServiceSession;
            var internalAuthResult = await _autoServiceAuthenticator.AuthenticateAsync(ipsInternalAuthProto);
            await using var stream = (RecyclableMemoryStream)_streamManager.GetStream("ipcInternalAuthentication", 4096, true);
            stream.ExWrite(internalAuthResult);
            await session.ReplyAsync(message.Id, stream, cancellationToken);
            taskCompletionSource.SetResult();
            if(!internalAuthResult) 
            {{
                _logger?.LogError("{{identity}}: Authentication failed for {{sessionId}}", _identity, session.Id);
                //Some time for client read the response
                await Task.Delay(System.TimeSpan.FromSeconds(10), cancellationToken);
                session.Disconnect();
                return;
            }}
            _logger?.LogInformation("{{identity}}: Authentication succeeded for {{sessionId}}", _identity, session.Id);
            return;
        }}