using JetBrains.Annotations;
using Microsoft.Msagl.Drawing;

namespace DogmaSolutions.ChangeImpactAnalysis.Sample01;

public sealed class MyProductImpactAnalyzer : ImpactAnalyzer<ArchitectureGraphGenerator, GitCommitsTracker, ArchitectureGraphDecorator>
{
    public MyProductImpactAnalyzer([NotNull] IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override Task<Graph> CreateArchitectureGraph(
        IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        CancellationToken cancellationToken)
    {
        var architectureGraph = new Graph("MyProduct");
        architectureGraph.AddEdge("MyProduct", "BusinessLogics.A");
        architectureGraph.AddEdge("MyProduct", "BusinessLogics.B");
        architectureGraph.AddEdge("MyProduct", "BusinessLogics.C");
        architectureGraph.AddEdge("MyProduct", "DataAccess.A");
        architectureGraph.AddEdge("MyProduct", "DataModels.B");
        architectureGraph.AddEdge("MyProduct", "CommonUtils.A");

        architectureGraph.AddEdge("BusinessLogics.A", "DataAccess.A");
        architectureGraph.AddEdge("BusinessLogics.A", "DataAccess.B");
        architectureGraph.AddEdge("BusinessLogics.B", "DataAccess.B");
        architectureGraph.AddEdge("BusinessLogics.C", "DataAccess.B");
        architectureGraph.AddEdge("BusinessLogics.C", "DataAccess.C");

        architectureGraph.AddEdge("BusinessLogics.B", "DataModels.A");
        architectureGraph.AddEdge("BusinessLogics.B", "DataModels.B");
        architectureGraph.AddEdge("BusinessLogics.C", "CommonUtils.A");
        architectureGraph.AddEdge("BusinessLogics.C", "CommonUtils.B");

        architectureGraph.AddEdge("DataAccess.A", "CommonUtils.A");
        architectureGraph.AddEdge("DataAccess.A", "DataModels.A");
        architectureGraph.AddEdge("DataAccess.A", "DataModels.B");

        architectureGraph.AddEdge("DataAccess.B", "CommonUtils.B");
        architectureGraph.AddEdge("DataAccess.B", "DataModels.A");
        architectureGraph.AddEdge("DataAccess.B", "DataModels.C");

        architectureGraph.AddEdge("DataAccess.C", "DataModels.B");

        architectureGraph.AddEdge("DataModels.A", "CommonUtils.A");
        architectureGraph.AddEdge("DataModels.B", "CommonUtils.B");
        architectureGraph.AddEdge("DataModels.B", "CommonUtils.C");
        architectureGraph.AddEdge("DataModels.C", "CommonUtils.A");
        architectureGraph.AddEdge("DataModels.C", "CommonUtils.B");

        return Task.FromResult(architectureGraph);
    }

    protected override ArchitectureGraphDecorator CreateArchitectureGraphDecorator()
    {
        return new MyProductArchitectureGraphDecorator(ServiceProvider);
    }

    protected override GitCommitsTracker CreateGitCommitsTracker()
    {
        return new MyProductGitCommitsTracker(ServiceProvider);
    }
}