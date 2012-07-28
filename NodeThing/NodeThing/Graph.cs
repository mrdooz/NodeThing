using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    [DataContract]
    public class Graph
    {
        void RenderNode(GraphNode root, Graphics g)
        {
            root.Node.Render(g);
            foreach (var child in root.Children) {
                if (child != null) {
                    RenderNode(child, g);
                }
            }

            var pen = new Pen(Color.Black, 2);
            var selectedPen = new Pen(Color.CornflowerBlue, 2);

            // render connections from node's input to output
            for (var i = 0; i < root.Children.Count(); ++i) {
                if (root.Children[i] == null)
                    continue;

                var child = root.Children[i];
                var cp0 = root.Node.ConnectionPos(Connection.Io.Input, i);
                var cp1 = child.Node.ConnectionPos(Connection.Io.Output, 0);
                if (cp0.Item1 && cp1.Item1) {
                    g.DrawLine(root.Node.Inputs[i].Selected && child.Node.Output.Selected ? selectedPen : pen, cp0.Item2, cp1.Item2);
                }
            }
        }

        public void Render(Graphics g)
        {
            foreach (var r in _roots)
                RenderNode(r, g);
        }

        public Node PointInsideNode(Point pt)
        {
            return _nodes.FirstOrDefault(node => node.PointInsideBody(pt));
        }

        public Connection PointInsideConnection(Point pt)
        {
            foreach (var node in _nodes) {
                var conn = node.PointInsideConnection(pt);
                if (conn != null)
                    return conn;
            }
            return null;
        }


        private Tuple<Connection, Connection> PointOnConnectionNode(Point pt, GraphNode parent)
        {
            var ptf = new PointF(pt.X, pt.Y);

            // Check if the point lies on the connection between a parent and any of its children
            for (int i = 0; i < parent.Children.Count(); ++i) {
                var child = parent.Children[i];
                if (child == null)
                    continue;

                var c0 = parent.Node.ConnectionPos(Connection.Io.Input, i).Item2;
                var c1 = child.Node.ConnectionPos(Connection.Io.Output, 0).Item2;

                var lineLength = PointMath.len(PointMath.sub(c1, c0));

                if (PointMath.len(PointMath.sub(ptf, c0)) <= lineLength && PointMath.len(PointMath.sub(ptf, c1)) <= lineLength) {
                    PointF p1 = new PointF(c0.X, c0.Y);
                    PointF p2 = new PointF(c1.X, c1.Y);

                    PointF r = new PointF(pt.X - p1.X, pt.Y - p1.Y);

                    // normal to the line
                    PointF v = new PointF(p2.Y - p1.Y, -(p2.X - p1.X));

                    // distance from r to line = dot(norm(v), r)
                    float d = Math.Abs(PointMath.dot(PointMath.normalize(v), r));

                    if (d < 2)
                        return new Tuple<Connection, Connection>(parent.Node.Inputs[i], child.Node.Output);
                }

                var childResult = PointOnConnectionNode(pt, parent.Children[i]);
                if (childResult.Item1 != null && childResult.Item2 != null)
                    return childResult;
            }
            return new Tuple<Connection, Connection>(null, null);
        }

        public Tuple<Connection, Connection> PointOnConnection(Point pt)
        {
            foreach (var r in _roots) {
                var res = PointOnConnectionNode(pt, r);
                if (res.Item1 != null && res.Item2 != null)
                    return res;
            }
            return new Tuple<Connection, Connection>(null, null);
        }

        private bool DeleteConnectionNode(GraphNode parent, Connection cParent, Connection cChild)
        {
            for (var i = 0; i < parent.Children.Count(); ++i) {
                var child = parent.Children[i];
                if (child == null)
                    continue;

                if (i == cParent.Slot && cParent.Node == parent.Node && cChild.Node == child.Node) {
                    parent.Children[i] = null;
                    parent.Node.Inputs[i].Used = false;

                    child.Parent = null;
                    child.Node.Output.Used = false;

                    // Add the nodes to the roots list if needed
                    if (parent.IsRootNode() && _roots.FirstOrDefault(a => a == parent) == null)
                        _roots.Add(parent);

                    if (child.IsRootNode() && _roots.FirstOrDefault(a => a == child) == null)
                        _roots.Add(child);

                    return true;
                }

                if (DeleteConnectionNode(child, cParent, cChild))
                    return true;
            }
            return false;
        }

        public void DeleteConnection(Connection parent, Connection child)
        {
            foreach (var r in _roots) {
                if (DeleteConnectionNode(r, parent, child))
                    return;
            }
        }

        public void DeleteNode(Node node)
        {
            var graphNode = FindNode(node);

            // Disconnect from any children
            foreach (var c in graphNode.Children) {
                if (c != null) {
                    c.Parent = null;
                    c.Node.Output.Used = false;
                    _roots.Add(c);
                }
            }

            // Disconnect from parent
            if (graphNode.Parent != null) {
                for (var i = 0; i < graphNode.Parent.Children.Count(); ++i) {
                    if (graphNode.Parent.Children[i] == graphNode) {
                        graphNode.Parent.Children[i] = null;
                        graphNode.Parent.Node.Inputs[i].Used = false;
                        break;
                    }
                }
            }

            // Remove from roots and nodes
            _roots.RemoveAll(a => a == graphNode);
            _nodes.RemoveAll(a => a == node);
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
                        return f;
                }
            }

            return null;
        }

        public void AddConnection(Node parent, int parentSlot, Node child, int childSlot)
        {
            var c = FindNode(child);
            var p = FindNode(parent);

            p.Children[parentSlot] = c;
            c.Parent = p;

            _roots.RemoveAll(a => a == c);
        }

        [DataMember]
        List<GraphNode> _roots = new List<GraphNode>();

        [DataMember]
        List<Node> _nodes = new List<Node>();
    }

    [DataContract(IsReference = true)]
    public class GraphNode
    {
        public GraphNode(Node n)
        {
            Node = n;
            Children = new GraphNode[n.Inputs.Count];
        }

        public bool IsRootNode()
        {
            // A node is a root node if it doesn't have a parent
            return Parent == null;
        }

        [DataMember]
        public Node Node { get; set; }

        [DataMember]
        public GraphNode Parent { get; set; }

        [DataMember]
        public GraphNode[] Children { get; private set; }
    }
}
