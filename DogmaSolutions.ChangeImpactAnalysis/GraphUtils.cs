using System;
using System.Collections.Generic;
using DogmaSolutions.Collections;
using JetBrains.Annotations;
using Microsoft.Msagl.Drawing;

namespace DogmaSolutions.ChangeImpactAnalysis;

public static class GraphUtils
{
    public static void MarkAsChanged([NotNull] Graph graph, [NotNull] string nodeId, Microsoft.Msagl.Drawing.Color borderColor, Microsoft.Msagl.Drawing.Color edgeColor, [NotNull] List<string> markedNodes)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (markedNodes == null) throw new ArgumentNullException(nameof(markedNodes));
        if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("Node Id cannot be null or whitespace.", nameof(nodeId));

        markedNodes.AddDistinct(nodeId, false);

        var n = graph.FindNode(nodeId);
        n.Attr.Color = borderColor;
        foreach (var inEdge in n.InEdges)
        {
            inEdge.Attr.Color = edgeColor;
            if (!string.IsNullOrWhiteSpace(inEdge.Source))
                MarkAsChanged(graph, inEdge.Source, borderColor, edgeColor, markedNodes);
        }
    }
}