using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IGitCommitsTracker
{
    Task<IEnumerable<string>> GetImpactedComponents(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken = default);
}