using System.Collections.Generic;
using System.Text;

namespace sly.parser.generator.visitor.dotgraph
{
    public class DotGraph
    {
        private readonly string graphName;
        private readonly bool directed;
        private List<DotNode> nodes;
        private List<DotArrow> edges;

        public DotGraph(string graphName, bool directed)
        {
            this.graphName = graphName;
            this.directed = directed;
            nodes = new List<DotNode>();
            edges = new List<DotArrow>();
        }

        public void Add(DotNode node)
        {
            nodes.Add(node);
        }

        public void Add(DotArrow edge)
        {
            edges.Add(edge);
        }

        public string Compile()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(directed ? "digraph" : "graph");
            builder.AppendLine($" {graphName} {{");
            foreach (var node in nodes)
            {
                builder.AppendLine(node.ToGraph());
            }

            foreach (var edge in edges)
            {
                builder.AppendLine(edge.ToGraph());
            }

            builder.AppendLine("}");
            return builder.ToString();
        }
    }
}