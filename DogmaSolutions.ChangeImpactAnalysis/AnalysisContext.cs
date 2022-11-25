using System.Threading.Tasks;
using DogmaSolutions.Tasking;
using DogmaSolutions.Utils;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class AnalysisContext : IAnalysisContextEvents, IAnalysisContextEventTriggers
{
    public event AsyncEventHandler<ValueEventArgs<int>> Progress;
    public event AsyncEventHandler<UserMessageEventArgs> Message;
    public event AsyncEventHandler<UserQuestionEventArgs<bool>> BooleanUserInteraction;


    public virtual Task OnProgress(ValueEventArgs<int> e)
    {
        Progress?.Invoke(this, e).RunAndForget();
        return Task.CompletedTask;
    }

    public virtual Task OnMessage(UserMessageEventArgs e)
    {
        Message?.Invoke(this, e).RunAndForget();
        return Task.CompletedTask;
    }

    public virtual async Task OnBooleanUserInteraction(UserQuestionEventArgs<bool> e)
    {
        var evt = BooleanUserInteraction;
        if (evt != null)
        {
            await evt.Invoke(this, e).ConfigureAwait(false);
        }
    }
}