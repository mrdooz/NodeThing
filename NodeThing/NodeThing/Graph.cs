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
        [DataMember]
        List<GraphNode> _roots = new List<GraphNode>();

        [DataMember]
        List<Node> _nodes = new List<Node>();

        private Pen _connectionPen;
        private Pen _selectedConnectionPen;

        public Graph()
        {
            var nullContext = new StreamingContext();
            OnDeserializerd(nullContext);
        }

        [OnDeserialized]
        private void OnDeserializerd(StreamingContext sc)
        {
            _connectionPen = new Pen(Color.Black, 2);
            _selectedConnectionPen = new Pen(Color.CornflowerBlue, 2);
        }


        private void RenderNode(GraphNode root, Graphics g, MainForm.ClientTransform transform)
        {
            root.Node.Render(g, transform);

            foreach (var child in root.Children) {
                if (child != null) {
                    RenderNode(child, g, transform);
                }
            }

            // render connections from node's input to output
            for (var i = 0; i < root.Children.Count(); ++i) {
                if (root.Children[i] == null)
                    continue;

                var child = root.Children[i];
                var cp0 = root.Node.ConnectionPos(Connection.Io.Input, i);
                var cp1 = child.Node.ConnectionPos(Connection.Io.Output, 0);
                if (cp0.Item1 && cp1.Item1) {
                    var scrolledCp0 = transform.PointToClient(cp0.Item2);
                    var scrolledCp1 = transform.PointToClient(cp1.Item2);
                    g.DrawLine(root.Node.Inputs[i].Selected && child.Node.Output.Selected ? _selectedConnectionPen : _connectionPen, scrolledCp0, scrolledCp1);
                }
            }
        }

        public void Render(Graphics g, MainForm.ClientTransform transform)
        {
            foreach (var r in _roots)
                RenderNode(r, g, transform);
        }

        public Node PointInsideNode(Point pt)
        {
            return _nodes.FirstOrDefault(node => node.PointInsideBody(pt));
        }

        public Rectangle GetBoundingRectangle()
        {
            if (_nodes.Count == 0)
                return new Rectangle(0, 0, 0, 0);

            var first = _nodes[0];
            var rect = first.BoundingRect();
            // Kinda silly, but the Pos doesn't take the connection blobs into account (but the BoundingRect does)
            var pos = new Point(rect.Left, rect.Top);
            var tl = pos;
            var br = new Point(rect.Right, rect.Bottom);
            for (var i = 1; i < _nodes.Count; ++i) {
                rect = _nodes[i].BoundingRect();
                pos = new Point(rect.Left, rect.Top);
                tl = PointMath.Min(tl, pos);
                br = PointMath.Max(br, new Point(rect.Right, rect.Bottom));
            }

            return new Rectangle(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
        }

        public List<Node> NodesInsideRect(Rectangle rect)
        {
            var res = new List<Node>();
            foreach (var n in _nodes) {
                var boundingRect = n.BoundingRect();
                if (rect.IntersectsWith(boundingRect))
                    res.Add(n);

            }
            return res;
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

        public void MoveNodes(int dx, int dy)
        {
            foreach (var n in _nodes) {
                var pt = n.Pos;
                n.Pos = new Point(pt.X + dx, pt.Y + dy);
            }
        }

        private Tuple<Connection, Connection> PointOnConnectionInner(Point pt, GraphNode parent)
        {
            var ptf = new PointF(pt.X, pt.Y);

            // Check if the point lies on the connection between a parent and any of its children
            for (int i = 0; i < parent.Children.Count(); ++i) {
                var child = parent.Children[i];
                if (child == null)
                    continue;

                var c0 = parent.Node.ConnectionPos(Connection.Io.Input, i).Item2;
                var c1 = child.Node.ConnectionPos(Connection.Io.Output, 0).Item2;

                var lineLength = PointMath.Len(PointMath.Sub(c1, c0));

                if (PointMath.Len(PointMath.Sub(ptf, c0)) <= lineLength && PointMath.Len(PointMath.Sub(ptf, c1)) <= lineLength) {
                    var p1 = new PointF(c0.X, c0.Y);
                    var p2 = new PointF(c1.X, c1.Y);

                    var r = new PointF(pt.X - p1.X, pt.Y - p1.Y);

                    // normal to the line
                    var v = new PointF(p2.Y - p1.Y, -(p2.X - p1.X));

                    // distance from r to line = dot(norm(v), r)
                    var d = Math.Abs(PointMath.Dot(PointMath.Normalize(v), r));

                    if (d < 2)
                        return new Tuple<Connection, Connection>(parent.Node.Inputs[i], child.Node.Output);
                }

                var childResult = PointOnConnectionInner(pt, parent.Children[i]);
                if (childResult.Item1 != null && childResult.Item2 != null)
                    return childResult;
            }
            return new Tuple<Connection, Connection>(null, null);
        }

        public Tuple<Connection, Connection> PointOnConnection(Point pt)
        {
            foreach (var r in _roots) {
                var res = PointOnConnectionInner(pt, r);
                if (res.Item1 != null && res.Item2 != null)
                    return res;
            }
            return new Tuple<Connection, Connection>(null, null);
        }

        private bool DeleteConnectionInner(GraphNode parent, Connection cParent, Connection cChild)
        {
            for (var i = 0; i < parent.Children.Count(); ++i) {
                var child = parent.Children[i];
                if (child == null)
                    continue;

                if (i == cParent.Slot && cParent.Node == parent.Node && cChild.Node == child.Node) {
                    parent.Children[i] = null;
                    parent.Node.Inputs[i].Used = false;

                    child.Parents.Remove(parent);
                    child.Node.Output.Used = false;

                    // Add the nodes to the roots list if needed
                    if (parent.IsRootNode() && _roots.FirstOrDefault(a => a == parent) == null)
                        _roots.Add(parent);

                    if (child.IsRootNode() && _roots.FirstOrDefault(a => a == child) == null)
                        _roots.Add(child);

                    return true;
                }

                if (DeleteConnectionInner(child, cParent, cChild))
                    return true;
            }
            return false;
        }

        public void DeleteConnection(Connection parent, Connection child)
        {
            foreach (var r in _roots) {
                if (DeleteConnectionInner(r, parent, child))
                    return;
            }
        }

        public void DeleteNode(Node node)
        {
            var graphNode = FindNode(node);

            // Disconnect from any children
            foreach (var c in graphNode.Children) {
                if (c != null) {
                    c.Parents.Remove(graphNode);
                    c.Node.Output.Used = false;
                    _roots.Add(c);
                }
            }

            // Disconnect from parents
            foreach (var p in graphNode.Parents) {
                for (var i = 0; i < p.Children.Count(); ++i) {
                    if (p.Children[i] == graphNode) {
                        p.Children[i] = null;
                        p.Node.Inputs[i].Used = false;
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
            c.Parents.Add(p);

            _roots.RemoveAll(a => a == c);
        }

        private GraphNode FindSelectedNode(GraphNode node)
        {
            if (node.Node.Selected)
                return node;

            foreach (var c in node.Children.Where(c => c != null)) {
                var res = FindSelectedNode(c);
                if (res != null)
                    return res;
            }
            return null;
        }

        public GeneratorSequence GenerateCodeFromSelected(Node selected)
        {
            GraphNode root;
            if (selected.IsSink()) {
                // If selecting a sink, use its child instead as sinks are really just sentinels..
                root = selected.Inputs.Count == 1 ? FindNode(selected).Children[0] : null;
            } else {
                root = FindNode(selected);
            }

            return root != null ? TopologicalSorter.SequenceFromNode(root) : new GeneratorSequence();
        }

        private List<GraphNode> FindRoots(GraphNode node)
        {
            return null;
            var res = new List<GraphNode>();
            var nodes = new Stack<GraphNode>();
            nodes.Push(node);
            while (nodes.Count > 0) {
                var cur = nodes.Pop();
/*
                if (cur.Parent == null) {
                    
                }
 */
            }

            //if (node.Parent )
        }
/*
        public List<GeneratorSequence> GenerateCodeFromSelected2(Node selected)
        {
            // Generates sequences for the selected node, as well as any roots in the graph
            GraphNode root;
            if (selected.IsSink()) {
                // If selecting a sink, use its child instead as sinks are really just sentinels..
                root = selected.Inputs.Count == 1 ? FindNode(selected).Children[0] : null;
            } else {
                root = FindNode(selected);
            }

            return root != null ? TopologicalSorter.SequenceFromNode(root) : new GeneratorSequence();
        }
*/

        public List<GeneratorSequence> GenerateCode()
        {
            // Generates sequences for all the roots in the current graph
            var res = new List<GeneratorSequence>();

            foreach (var r in _roots) {

                if (r.Node.IsSink()) {
                    var root = r.Children[0];
                    if (root != null) {
                        var selectedChild = FindSelectedNode(root);

                        var seq = TopologicalSorter.SequenceFromNode(root);
                        seq.IsPreview = false;
                        res.Add(seq);
                        if (selectedChild != null) {
                            var previewSeq = TopologicalSorter.SequenceFromNode(selectedChild);
                            previewSeq.IsPreview = true;
                            res.Add(previewSeq);
                        }
                    }
                } else {
                    // Node isn't a proper sink, so we generate a preview
                    var seq = TopologicalSorter.SequenceFromNode(r);
                    seq.IsPreview = true;
                    res.Add(seq);
                }

            }
            return res;
        }
    }

    [DataContract(IsReference = true)]
    public class GraphNode
    {
        public GraphNode(Node n)
        {
            Node = n;
            Children = new GraphNode[n.Inputs.Count];
            Parents = new List<GraphNode>();
        }

        [DataMember]
        public Node Node { get; set; }

        [DataMember]
        public List<GraphNode> Parents { get; set; }

        [DataMember]
        public GraphNode[] Children { get; private set; }


        public bool IsRootNode()
        {
            // A node is a root node if it doesn't have a parent
            return Parents.Count == 0;
        }
    }
}
