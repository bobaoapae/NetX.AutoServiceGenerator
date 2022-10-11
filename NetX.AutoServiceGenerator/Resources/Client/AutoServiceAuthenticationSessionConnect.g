        var authenticationResult = await _processor.SendAuthentication(_netXClient, ipsInternalAuthenticationProto, cancellationToken);
        if(!authenticationResult)
        {{
            _logger?.LogError("{{identity}}:  Authentication failed", _identity);
            Disconnect();
        }}