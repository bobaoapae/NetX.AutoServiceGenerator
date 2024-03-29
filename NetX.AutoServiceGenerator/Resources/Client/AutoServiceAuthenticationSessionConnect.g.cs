        var authenticationResult = await _processor.SendAuthentication(_netXClient, ipsInternalAuthenticationProto, cancellationToken);
        if(!authenticationResult.IsAuthenticated)
        {{
            _logger?.LogError("{{identity}}:  Authentication failed", _identity);
            Disconnect();
            throw new Exception($"{{_identity}}:  Authentication failed");
        }}
        await OnAuthenticatedAsync(authenticationResult);