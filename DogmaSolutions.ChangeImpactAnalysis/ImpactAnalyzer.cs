using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DogmaSolutions.PrimitiveTypes;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Msagl.Drawing;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ImpactAnalyzer : ImpactAnalyzer<ArchitectureGraphGenerator, GitCommitsTracker, ArchitectureGraphDecorator>
{
    public ImpactAnalyzer([NotNull] IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}

public class ImpactAnalyzer<TArchitectureGraphGenerator, TGitCommitsTracker, TArchitectureGraphDecorator> : IImpactAnalyzer
where TArchitectureGraphGenerator : class, IArchitectureGraphGenerator
where TGitCommitsTracker : class, IGitCommitsTracker
where TArchitectureGraphDecorator : class, IArchitectureGraphDecorator
{
    [NotNull] private readonly ILogger<ImpactAnalyzer> _logger;
    [NotNull] protected IServiceProvider ServiceProvider { get; }

    public ImpactAnalyzer([NotNull] IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<ImpactAnalyzer>>();
    }

    public virtual async Task<ImpactAnalysisDescriptor> Analyze(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating architecture graph");
        var architectureGraph = await CreateArchitectureGraph(architecture, parameters, eventTriggers, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Analyzing GIT commits to infer impacted components");
        var impactedComponents = await InferImpactedComponents(architecture, parameters, eventTriggers, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Found {Count} impacted components", impactedComponents?.Count ?? 0);

        _logger.LogInformation("Decorating the architectural tree with the informations related to the impacted components");
        var markedNodes = await DecorateArchitectureGraph(
                 architecture,
                 parameters,
                 eventTriggers,
                 architectureGraph,
                 impactedComponents,
                 cancellationToken).
             ConfigureAwait(false);

        _logger.LogInformation("Analysis completed, returning the results");
        var retVal = new ImpactAnalysisDescriptor();
        retVal.ArchitectureGraph = architectureGraph;
        retVal.ImpactedComponents = impactedComponents;
        retVal.MarkedComponents = markedNodes.Distinct(StringComparer.InvariantCultureIgnoreCase).OrderBy(s => s).ToList();
        return retVal;
    }

    protected virtual async Task<List<string>> DecorateArchitectureGraph(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        Graph architectureGraph,
        List<string> impactedComponents,
        CancellationToken cancellationToken)
    {
        var architectureGraphDecorator = CreateArchitectureGraphDecorator();
        return await architectureGraphDecorator.Decorate(architecture, parameters, eventTriggers, architectureGraph, impactedComponents, cancellationToken).ConfigureAwait(false);
    }

    protected virtual TArchitectureGraphDecorator CreateArchitectureGraphDecorator()
    {
        return (TArchitectureGraphDecorator)Activator.CreateInstance(typeof(TArchitectureGraphDecorator), new object[] { ServiceProvider });
    }

    protected virtual async Task<List<string>> InferImpactedComponents(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken)
    {
        var gitCommitsTracker = CreateGitCommitsTracker();
        var impactedComponents = (await gitCommitsTracker.GetImpactedComponents(architecture, parameters, eventTriggers, cancellationToken).ConfigureAwait(false)).ToList();
        return impactedComponents;
    }

    protected virtual TGitCommitsTracker CreateGitCommitsTracker()
    {
        return (TGitCommitsTracker)Activator.CreateInstance(typeof(TGitCommitsTracker), new object[] { ServiceProvider });
    }

    protected virtual async Task<Graph> CreateArchitectureGraph(
        IArchitecture architecture,
        [NotNull] ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken)
    {
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        var canUseAlreadyGeneratedFiles = false;
        if (!string.IsNullOrWhiteSpace(parameters.ArtifactsBaseFolderPath))
        {
            var artifactsBaseFolder = new DirectoryInfo(parameters.ArtifactsBaseFolderPath);
            var fn = parameters.ArchitectureGraphFileName;
            canUseAlreadyGeneratedFiles = artifactsBaseFolder.Exists && !string.IsNullOrWhiteSpace(fn);
            if (canUseAlreadyGeneratedFiles)
            {
                var searchPattern = parameters.ArchitectureGraphFileName.RemoveSuffix(".json") + "_ArchitectureOnly_*.msagl";
                var graphFile = artifactsBaseFolder.GetFiles(searchPattern).MaxBy(n => n.Name);
                if (graphFile != null && eventTriggers != null)
                {
                    var question = new UserQuestionEventArgs<bool>(
                        $"The file '{graphFile.Name}' was found. Do you want to re-use this, instead of regenerating all the architectural graph from scratch ?",
                        LogLevel.Information,
                        false);

                    // ask the user if the existing file must be used instead
                    await eventTriggers.OnBooleanUserInteraction(question).ConfigureAwait(false);

                    // did the user approve to use the exising file ?
                    if (question.Answer)
                    {
                        var architectureGraph = Graph.Read(graphFile.FullName);
                        return architectureGraph;
                    }
                }
            }
        }

        #region generate an architecture graph file from scratch

        var architectureGraphGenerator = CreateArchitectureGraphGenerator();
        var newArchitectureGraph = await GenerateArchitectureGraph(architectureGraphGenerator, architecture, parameters, eventTriggers, cancellationToken).ConfigureAwait(false);

        if (newArchitectureGraph != null && newArchitectureGraph.NodeCount > 0 && newArchitectureGraph.EdgeCount > 0)
        {
            if (canUseAlreadyGeneratedFiles)
            {
                var newFileName = parameters.ArchitectureGraphFileName.RemoveSuffix(".json") + "_ArchitectureOnly_" + DateTime.UtcNow.Ticks + ".msagl";
                var newFilePath = Path.Combine(parameters.ArtifactsBaseFolderPath, newFileName);
                newArchitectureGraph.Write(newFilePath);
            }
        }

        return newArchitectureGraph;

        #endregion
    }

    protected virtual async Task<Graph> GenerateArchitectureGraph(
        [NotNull] TArchitectureGraphGenerator architectureGraphGenerator,
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken)
    {
        if (architectureGraphGenerator == null) throw new ArgumentNullException(nameof(architectureGraphGenerator));

        return await architectureGraphGenerator.GenerateArchitectureGraph(
                 architecture,
                 parameters,
                 null,
                 null,
                 null,
                 null,
                 eventTriggers,
                 cancellationToken).
             ConfigureAwait(false);
    }

    protected virtual TArchitectureGraphGenerator CreateArchitectureGraphGenerator()
    {
        return (TArchitectureGraphGenerator)Activator.CreateInstance(typeof(TArchitectureGraphGenerator), new object[] { ServiceProvider });
    }
}