using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    public class Graph
    {
        void RenderRoot(GraphNode root, Graphics g)
        {
            root.Node.Render(g);
            foreach (var child in root.Children) {
                if (child != null) {
                    RenderRoot(child, g);
                }
            }
        }

        public void Render(Graphics g)
        {
            foreach (var r in _roots)
                RenderRoot(r, g);

            g.ResetTransform();
        }

        public Node PointInsideNode(Point pt)
        {
            return _nodes.FirstOrDefault(node => node.PointInsideBody(pt));
        }

        public Connection PointInsideConnection(Point pt)
        {
            foreach (var node in _nodes)
            {
                var conn = node.PointInsideConnection(pt);
                if (conn != null)
                    return conn;
            }
            return null;
        }

        public void AddNode(Node node)
        {
            _nodes.Add(node);
            _roots.Add(new GraphNode(node));
        }

        GraphNode FindNode(Node node)
        {
            foreach (var r in _roots) {
                var f = FindNodeInner(r, node);
                if (f != null)
                    return f;
            }
            return null;
        }

        GraphNode FindNodeInner(GraphNode root, Node node)
        {
            if (root.Node == node)
                return root;

            foreach (var child in root.Children) {
                if (child != null) {
                    var f = FindNodeInner(child, node);
                    if (f != null)
                        return child;
                }
            }

            return null;
        }

        public void AddConnection(Node parent, int parentSlot, Node child, int childSlot)
        {
            var c = FindNode(child);
            var p = FindNode(parent);

            p.Children[parentSlot] = c;

            // remove the child from the list of roots
            _roots.RemoveAll(a => a.Node == child);

        }

        List<GraphNode> _roots = new List<GraphNode>();
        List<Node> _nodes = new List<Node>();
    }

    public class GraphNode
    {
        public GraphNode(Node n)
        {
            Node = n;
            Children = new GraphNode[n.Inputs.Count];
        }

        public Node Node { get; set; }
        public GraphNode[] Children { get; private set; }
    }
}
