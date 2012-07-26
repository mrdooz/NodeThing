using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    public class Graph
    {
        public void render(Graphics g)
        {
            foreach (var node in _nodes)
            {
                node.render(g);
            }
        }

        public void addNode(Node node)
        {
            _nodes.Add(node);
        }

        List<Node> _nodes = new List<Node>();
    }

    public class GraphNode
    {
        Node _node;
        List<Node> _children;
    }
}
