using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DogmaSolutions.PrimitiveTypes;
using JetBrains.Annotations;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Node = Microsoft.Msagl.Drawing.Node;

namespace DogmaSolutions.ChangeImpactAnalysis;

public class ArchitectureGraphDecorator : IArchitectureGraphDecorator
{
    [NotNull] protected IServiceProvider ServiceProvider { get; }

    public ArchitectureGraphDecorator([NotNull] IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task<List<string>> Decorate(
        [NotNull] IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        [NotNull] Graph architectureGraph,
        List<string> impactedComponents,
        CancellationToken cancellationToken = default)
    {
        if (architecture == null) throw new ArgumentNullException(nameof(architecture));
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));

        foreach (var layer in architecture.Layers)
        {
            var nodesInLayer = GetNodesInLayer(architectureGraph, layer);
            ProcessNodesInLayer(architectureGraph, layer, nodesInLayer);
        }

        var markedNodes = DecorateImpactedNodes(architecture, architectureGraph, impactedComponents);
        ApplyStyle(architecture, parameters, eventTriggers, architectureGraph, impactedComponents, cancellationToken);

        return Task.FromResult(markedNodes);
    }

    protected virtual void ApplyStyle([NotNull] IArchitecture architecture,
        ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        [NotNull] Graph architectureGraph,
        List<string> impactedComponents,
        CancellationToken cancellationToken = default)
    {
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));

        architectureGraph.Attr.LayerDirection = LayerDirection.LR;
        architectureGraph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings();
        architectureGraph.LayoutAlgorithmSettings.PackingMethod = PackingMethod.Columns;
        architectureGraph.LayoutAlgorithmSettings.NodeSeparation = 30;
        architectureGraph.LayoutAlgorithmSettings.ClusterMargin = 50;
        architectureGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.SugiyamaSplines;
        architectureGraph.LayoutAlgorithmSettings.EdgeRoutingSettings.ConeAngle = 5;
    }

    protected virtual List<string> DecorateImpactedNodes([NotNull] IArchitecture architecture, [NotNull] Graph architectureGraph, List<string> impactedComponents)
    {
        if (architecture == null) throw new ArgumentNullException(nameof(architecture));
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));
        var topMostLayer = architecture.Layers.Last();
        var impactedNodeBorder = ToMsaglColor(topMostLayer.VisualAttributes.ImpactedNodeBorder);
        var impactedEdgeColor = ToMsaglColor(topMostLayer.VisualAttributes.ImpactedEdgeColor);
        var markedNodes = new List<string>();
        foreach (var node in architectureGraph.Nodes)
        {
            if (IsImpacted(impactedComponents, node))
            {
                GraphUtils.MarkAsChanged(architectureGraph, node.Id, impactedNodeBorder, impactedEdgeColor, markedNodes);
            }
        }

        return markedNodes;
    }

    protected virtual void ProcessNodesInLayer([NotNull] Graph architectureGraph, [NotNull] ArchitectureLayer layer, [NotNull] IEnumerable<Node> nodesInLayer)
    {
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        if (nodesInLayer == null) throw new ArgumentNullException(nameof(nodesInLayer));

        var visualAttributes = layer.VisualAttributes ?? new ArchitectureLayerVisualAttributes();
        var nodeBackground = ToMsaglColor(visualAttributes.NodeBackground);
        var nodeBorder = ToMsaglColor(visualAttributes.NodeBorder);

        var nodes = nodesInLayer.ToArray();
        foreach (var n in nodes)
        {
            n.Attr.FillColor = nodeBackground;
            n.Attr.Color = nodeBorder;
            n.Attr.Shape = Shape.Box;
            n.Attr.LabelMargin = 5;
        }

        architectureGraph.LayerConstraints.AddSameLayerNeighbors(nodes);
    }

    protected virtual IEnumerable<Node> GetNodesInLayer([NotNull] Graph architectureGraph, [NotNull] ArchitectureLayer layer)
    {
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));
        if (layer == null) throw new ArgumentNullException(nameof(layer));
        var solutionFile = new FileInfo(layer.SolutionFileLocation);
        //TODO: Improve resolution logic
        var baseFolder = solutionFile.Directory.FullName;
        var projectsNames = GetProjectsNames(solutionFile.Directory);
        var nodesInLayer = architectureGraph.Nodes.Where(n => projectsNames.Contains(n.LabelText, StringComparer.InvariantCultureIgnoreCase) == true);
        return nodesInLayer;
    }

    protected virtual bool IsImpacted(List<string> impactedComponents, [NotNull] Node node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        return impactedComponents?.Contains(node.Id, StringComparer.InvariantCultureIgnoreCase) == true ||
               impactedComponents?.Contains(node.LabelText, StringComparer.InvariantCultureIgnoreCase) == true;
    }

    protected virtual Microsoft.Msagl.Drawing.Color ToMsaglColor(System.Drawing.Color color)
    {
        return new Microsoft.Msagl.Drawing.Color(color.A, color.R, color.G, color.B);
    }

    protected virtual string[] GetProjectsNames([NotNull] DirectoryInfo folder)
    {
        if (folder == null) throw new ArgumentNullException(nameof(folder));

        var projectsNames = folder.
            GetFiles("*.csproj", SearchOption.AllDirectories).
            Select(fi => fi.Name.RemoveSuffix(".csproj", StringComparison.InvariantCultureIgnoreCase)).
            ToArray();

        return projectsNames;
    }
}