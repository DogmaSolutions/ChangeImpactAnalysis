using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DogmaSolutions.Collections;
using JetBrains.Annotations;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class GitCommitsTracker : IGitCommitsTracker
{
    [NotNull] protected IServiceProvider ServiceProvider { get; }

    public GitCommitsTracker([NotNull] IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public virtual Task<IEnumerable<string>> GetImpactedComponents(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken = default)
    {
        var retList = new List<string>();

        var nodes = parameters?.Filters?.ForcedNodes?.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (nodes is { Length: > 0 })
        {
            retList.AddRangeDistinct(nodes, false);
        }

        //TODO: Implement logics
        return Task.FromResult<IEnumerable<string>>(retList);
    }
}