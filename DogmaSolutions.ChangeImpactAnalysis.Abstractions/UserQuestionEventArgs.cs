using Microsoft.Extensions.Logging;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class UserQuestionEventArgs<T> : UserMessageEventArgs
{
    public T Answer { get; set; }

    public UserQuestionEventArgs(string question, LogLevel logLevel, T answer = default) : base(question, logLevel)
    {
        Answer = answer;
    }
}