using Microsoft.Msagl.Drawing;

namespace DogmaSolutions.ChangeImpactAnalysis.Sample01;

public sealed class MyProductArchitectureGraphDecorator : ArchitectureGraphDecorator
{
    protected override IEnumerable<Node> GetNodesInLayer(Graph architectureGraph, ArchitectureLayer layer)
    {
        if (architectureGraph == null) throw new ArgumentNullException(nameof(architectureGraph));
        var nodes = architectureGraph.Nodes.Where(n => n.LabelText.StartsWith(layer.IdentifiersPrefix + ".", StringComparison.InvariantCultureIgnoreCase) || 
                                                       string.Equals(n.LabelText, layer.IdentifiersPrefix, StringComparison.InvariantCultureIgnoreCase)).ToArray();
        return nodes;
    }

    public MyProductArchitectureGraphDecorator(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}