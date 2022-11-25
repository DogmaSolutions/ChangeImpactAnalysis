using System.Threading;
using System.Threading.Tasks;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IImpactAnalyzer
{
    Task<ImpactAnalysisDescriptor> Analyze(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken = default);
}