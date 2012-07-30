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

        public void SetPropertyListener(EventHandler h)
        {
            foreach (var n in _nodes) {
                n.SetPropertyListener(h);
            }
        }

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

        private CompNode CreateGraph(GraphNode root, CompNode parent, Dictionary<GraphNode, CompNode> nodes, ref List<CompNode> leaf)
        {
            CompNode node;
            bool newNode = false;

            // Check if the node has already been added
            if (!nodes.TryGetValue(root, out node)) {
                node = new CompNode { Depth = 0, Node = root };
                nodes.Add(root, node);
                newNode = true;
            } 
            
            if (parent != null)
                node.Parents.Add(parent);

            if (newNode) {
                foreach (var c in root.Children) {
                    if (c != null) {
                        node.Children.Add(CreateGraph(c, node, nodes, ref leaf));
                    }
                }
            }

            if (node.Children.Count == 0)
                leaf.Add(node);

            return node;
        }

        private int SetDepth(CompNode root, CompNode parent)
        {
            // To get the topological sorting I want, we set the depth of each node to the max of its children
            if (root.Children.Count == 0) {
                root.Depth = parent == null ? 0 : parent.Depth + 1;
            } else {
                int d = 0;
                foreach (var c in root.Children) {
                    d = Math.Max(d, SetDepth(c, root));
                }
                root.Depth = d;
            }
            return root.Depth;
        }

        public List<CompNode> TopologicalSort(CompNode g, List<CompNode> leaf)
        {
            var res = new List<CompNode>();
            var processed = new HashSet<CompNode>();
            while (leaf.Count > 0) {

                // pick the leaf with the greatest depth
                leaf.Sort((a, b) => a.Depth > b.Depth ? -1 : 1);
                var head = leaf[0];
                res.Add(head);
                leaf.RemoveAt(0);

                processed.Add(head);
                // Check if any of the head's parents are leaf now
                foreach (var p in head.Parents) {
                    bool isLeaf = p.Children.All(processed.Contains);
                    if (isLeaf)
                        leaf.Add(p);
                }

            }
            return res;
        }


        public GenerateSequence GenerateCodeFromSelected(Node selected, Size size, string name)
        {
            // If selecting a sink, use its child instead as sinks are really just sentinels..
            GraphNode root;
            if (selected.IsSink()) {
                if (selected.Inputs.Count != 1)
                    return new GenerateSequence();
                size = selected.GetProperty<Size>("Size");
                name = selected.GetProperty<string>("Name");
                root = FindNode(selected).Children[0];
            } else {
                root = FindNode(selected);
            }
            if (root == null)
                return new GenerateSequence();
            return GenerateCodeInner(root, size, name);
        }

        private GenerateSequence GenerateCodeInner(GraphNode root, Size size, string name)
        {
            var nodes = new Dictionary<GraphNode, CompNode>();
            var leaf = new List<CompNode>();
            var g = CreateGraph(root, null, nodes, ref leaf);
            SetDepth(g, null);
            var sorted = TopologicalSort(g, leaf);

            // Output the actual processing and texture allocation
            var completedNodes = new HashSet<CompNode>();
            var textureCache = new List<int>();
            var nextTextureIdx = 0;

            var candidates = new List<Tuple<int, CompNode>>();

            var sequence = new List<SequenceStep>();
            var usedTextures = new Dictionary<CompNode, int>();

            foreach (var s in sorted) {
                // Allocate a texture for s
                int textureIdx;
                if (textureCache.Count > 0) {
                    textureIdx = textureCache[0];
                    textureCache.RemoveAt(0);
                } else {
                    textureIdx = nextTextureIdx++;
                }

                var cur = new Tuple<int, CompNode>(textureIdx, s);
                var inputTextures = new List<int>();
                foreach (var c in s.Children) {
                    int texture;
                    if (usedTextures.TryGetValue(c, out texture)) {
                        inputTextures.Add(texture);
                    }
                }

                sequence.Add(new SequenceStep { TextureIdx = textureIdx, Node = s.Node.Node, InputTextures = inputTextures });
                completedNodes.Add(s);
                usedTextures.Add(s, textureIdx);

                // Check if any of the candidates have all their parents done, in which case they
                // can recycle their texture
                for (var i = 0; i < candidates.Count; ++i) {
                    var cand = candidates[i];
                    bool safeToRemove = cand.Item2.Parents.All(completedNodes.Contains);
                    if (safeToRemove) {
                        textureCache.Add(cand.Item1);
                        candidates.RemoveAt(i);
                    }

                }

                candidates.Add(cur);
            }
            return new GenerateSequence() { Name = name, NumTexture = nextTextureIdx, Sequence = sequence, Size = size };
        }

        private GraphNode FindSelectedChild(GraphNode node)
        {
            if (node.Node.Selected)
                return node;

            foreach (var c in node.Children) {
                if (c != null) {
                    var res = FindSelectedChild(c);
                    if (res != null)
                        return res;
                }
            }
            return null;
        }

        public List<GenerateSequence> GenerateCode()
        {
            var res = new List<GenerateSequence>();

            // Create a graph from each node in the root-list that is a sink (actually from the sink's child)
            foreach (var r in _roots) {

                if (r.Node.IsSink()) {
                    var root = r.Children[0];
                    if (root != null) {
                        var size = r.Node.GetProperty<Size>("Size");
                        var name = r.Node.GetProperty<string>("Name");
                        var selectedChild = FindSelectedChild(root);

                        var seq = GenerateCodeInner(root, size, name);
                        seq.IsPreview = false;
                        res.Add(seq);
                        if (selectedChild != null) {
                            var previewSeq = GenerateCodeInner(selectedChild, size, name);
                            previewSeq.IsPreview = true;
                            res.Add(previewSeq);
                        }
                    }
                } else {
                    // Node isn't a proper sink, so we generate a preview
                    var size = new Size(512, 512);
                    var name = "Preview";
                    var seq = GenerateCodeInner(r, size, name);
                    res.Add(seq);
                }

            }
            return res;
        }
    }

    public class SequenceStep
    {
        public int TextureIdx { get; set; }
        public Node Node { get; set; }
        public List<int> InputTextures { get; set; }
    }

    public class GenerateSequence
    {
        public GenerateSequence()
        {
            Sequence = new List<SequenceStep>();
        }

        public bool IsPreview { get; set; }

        public Size Size { get; set; }
        public string Name { get; set; }
        public List<SequenceStep> Sequence { get; set; }
        public int NumTexture { get; set; }
    }

    public class CompNode
    {
        public CompNode()
        {
            Children = new List<CompNode>();
            Parents = new List<CompNode>();
        }

        public GraphNode Node { get; set; }
        public int Depth { get; set; }
        public List<CompNode> Parents { get; set; }
        public List<CompNode> Children { get; set; }
    }

    [DataContract(IsReference = true)]
    public class GraphNode
    {
        [DataMember]
        public Node Node { get; set; }

        [DataMember]
        public GraphNode Parent { get; set; }

        [DataMember]
        public GraphNode[] Children { get; private set; }

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
    }
}
