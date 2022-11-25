using System;
using Microsoft.Extensions.Logging;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class UserMessageEventArgs : EventArgs
{
    public string Message { get; }
    public Microsoft.Extensions.Logging.LogLevel LogLevel { get; }

    public UserMessageEventArgs(string message, LogLevel logLevel)
    {
        Message = message;
        LogLevel = logLevel;
    }
}