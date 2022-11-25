using System.Threading.Tasks;
using DogmaSolutions.Utils;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IAnalysisContextEventTriggers
{
    Task OnProgress(ValueEventArgs<int> e);

    Task OnMessage(UserMessageEventArgs e);

    Task OnBooleanUserInteraction(UserQuestionEventArgs<bool> e);
}