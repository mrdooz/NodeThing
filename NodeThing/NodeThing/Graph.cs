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
            g.ResetTransform();
        }

        public Node pointInsideNode(Point pt)
        {
            foreach (var node in _nodes)
            {
                if (node.pointInsideBody(pt))
                    return node;
            }
            return null;
        }

        public Connection pointInsideConnection(Point pt)
        {
            foreach (var node in _nodes)
            {
                var conn = node.pointInsideConnection(pt);
                if (conn != null)
                    return conn;
            }
            return null;
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
