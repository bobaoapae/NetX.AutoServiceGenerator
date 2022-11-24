        var taskCompletionSource = new TaskCompletionSource();
        if(!_pendingAuthentications.TryAdd(session.Id, taskCompletionSource))
        {{
            _logger?.LogError("{{identity}}: Fail on add pending authentication to session ({{sessionId}})", _identity, session.Id);
            session.Disconnect();
            return;
        }}

        var delayTask = Task.Delay(System.TimeSpan.FromSeconds(10), cancellationToken)
                        .ContinueWith(_ =>
                                        {{
                                            if (taskCompletionSource.Task.IsCompleted)
                                                return;
                                            
                                            taskCompletionSource.TrySetException(new TimeoutException());
                        
                                            if (!_pendingAuthentications.TryRemove(session.Id, out var __))
                                            {{
                                                _logger?.LogError("{{identity}}: Cannot remove task completion for MessageId = {{msgId}}", _identity, session.Id);
                                            }}
                                            
                                            _logger?.LogError("{{identity}}: Timeout on authentication for session ({{sessionId}})", _identity, session.Id);                                    

                                            session.Disconnect();
                                        }}, cancellationToken);
                        
        await Task.WhenAny(taskCompletionSource.Task, delayTask);