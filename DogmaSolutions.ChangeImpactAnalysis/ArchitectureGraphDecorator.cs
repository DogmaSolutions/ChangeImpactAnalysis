using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DogmaSolutions.PrimitiveTypes;
using JetBrains.Annotations;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Edge = Microsoft.Msagl.Drawing.Edge;
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
        [NotNull] ImpactAnalysisParameters parameters,
        IAnalysisContextEventTriggers eventTriggers,
        [NotNull] Graph architectureGraph,
        List<string> impactedComponents,
        CancellationToken cancellationToken = default)
    {
        if (architecture == null) throw new ArgumentNullException(nameof(architecture));
        if (parameters == null) throw new ArgumentNullException(nameof(parameters));
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));

        if (parameters.RemoveRedundantDependencies)
        {
            RemoveRedundantDependencies(architectureGraph);
        }

        foreach (var layer in architecture.Layers)
        {
            var nodesInLayer = GetNodesInLayer(architectureGraph, layer);
            ProcessNodesInLayer(architectureGraph, layer, nodesInLayer);
        }

        var markedNodes = DecorateImpactedNodes(architecture, architectureGraph, impactedComponents);
        ApplyStyle(architecture, parameters, eventTriggers, architectureGraph, impactedComponents, cancellationToken);

        return Task.FromResult(markedNodes);
    }

    protected virtual void RemoveRedundantDependencies([NotNull] Graph graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));

        foreach (var outerNode in graph.Nodes.ToArray())
        {
            var innerNodes = graph.Nodes.Where(n => !Equals(n, outerNode)).ToArray();
            foreach (var innerNode in innerNodes)
            {
                var allPaths = FindPaths(outerNode, innerNode);
                if (allPaths.Count > 1)
                {
                    var directPathExists = allPaths.Any(path => path.Count == 2);
                    if (directPathExists)
                    {
                        var edgesToRemove = graph.Edges.Where(edge => edge.Source == outerNode.Id && edge.Target == innerNode.Id).ToArray();
                        foreach (var edge in edgesToRemove)
                        {
                            graph.RemoveEdge(edge);
                        }
                    }
                }
            }
        }
    }

    protected virtual List<List<string>> FindPaths([NotNull] Node source, [NotNull] Node target)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target == null) throw new ArgumentNullException(nameof(target));

        var retPaths = new List<List<string>>();
        foreach (var sourceOutEdge in source.OutEdges)
        {
            var path = new List<string>();
            Navigate(retPaths, path, source.Id, target.Id, sourceOutEdge);
        }

        return retPaths;
    }

    private static void Navigate(List<List<string>> allPaths, List<string> currentPath, string startingSource, string searchedTarget, Edge currentEdge)
    {
        if (currentPath.Count == 0)
        {
            currentPath.Add(currentEdge.Source);
        }

        currentPath.Add(currentEdge.Target);

        if (currentEdge.Target == searchedTarget)
        {
            // match found
            allPaths.Add(currentPath);
            return;
        }

        // match not found
        foreach (var targetOutEdge in currentEdge.TargetNode.OutEdges)
        {
            var clonedPath = new List<string>(currentPath);
            Navigate(allPaths, clonedPath, startingSource, searchedTarget, targetOutEdge);
        }
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