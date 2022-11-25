using DogmaSolutions.Tasking;
using DogmaSolutions.Utils;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IAnalysisContextEvents
{
    event AsyncEventHandler<ValueEventArgs<int>> Progress;
    event AsyncEventHandler<UserMessageEventArgs> Message;
    event AsyncEventHandler<UserQuestionEventArgs<bool>> BooleanUserInteraction;
}