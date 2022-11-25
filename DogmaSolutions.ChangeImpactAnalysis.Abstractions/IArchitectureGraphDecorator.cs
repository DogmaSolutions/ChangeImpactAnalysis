using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Msagl.Drawing;

namespace DogmaSolutions.ChangeImpactAnalysis;

public interface IArchitectureGraphDecorator
{
    Task<List<string>> Decorate(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        Graph architectureGraph,
        List<string> impactedComponents,
        CancellationToken cancellationToken = default);
}