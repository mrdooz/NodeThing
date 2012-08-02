using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NodeThing
{
    class TopologicalSorter
    {
        class TopoNode
        {
            public TopoNode()
            {
                Children = new List<TopoNode>();
                Parents = new List<TopoNode>();
            }

            public GraphNode Node { get; set; }
            public int Depth { get; set; }
            public List<TopoNode> Parents { get; set; }
            public List<TopoNode> Children { get; set; }
        }


        static public GeneratorSequence SequenceFromNode(GraphNode root)
        {
            var top = new TopologicalSorter();
            return top.GenerateCode(root);
        }
       
        private TopoNode CreateGraph(GraphNode root, TopoNode parent, Dictionary<GraphNode, TopoNode> nodes, ref List<TopoNode> leaf)
        {
            TopoNode node;
            bool newNode = false;

            // Check if the node has already been added
            if (!nodes.TryGetValue(root, out node)) {
                node = new TopoNode { Depth = 0, Node = root };
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

        private int SetDepth(TopoNode root, TopoNode parent)
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

        private IEnumerable<TopoNode> TopologicalSort(GraphNode root)
        {
            var nodes = new Dictionary<GraphNode, TopoNode>();
            var leaf = new List<TopoNode>();
            var g = CreateGraph(root, null, nodes, ref leaf);
            SetDepth(g, null);

            var res = new List<TopoNode>();
            var processed = new HashSet<TopoNode>();
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

        private GeneratorSequence GenerateCode(GraphNode root)
        {
            var sorted = TopologicalSort(root);

            var completedNodes = new HashSet<TopoNode>();
            var textureCache = new Stack<int>(); // keeps track of texture indices we can reuse
            var nextTextureIdx = 0;

            var candidates = new List<Tuple<int, TopoNode>>();

            var sequence = new List<SequenceStep>();
            var usedTextures = new Dictionary<TopoNode, int>(); // maps node to destination texture index

            // Output the actual processing and texture allocation
            foreach (var s in sorted) {

                // Allocate a texture for s
                var textureIdx = textureCache.Count == 0 ? nextTextureIdx++ : textureCache.Pop();

                var cur = new Tuple<int, TopoNode>(textureIdx, s);
                var inputTextures = new List<int>();
                foreach (var c in s.Children) {
                    int texture;
                    if (usedTextures.TryGetValue(c, out texture)) {
                        inputTextures.Add(texture);
                    }
                }

                sequence.Add(new SequenceStep {DstTextureIdx = textureIdx, Node = s.Node.Node, InputTextures = inputTextures});
                completedNodes.Add(s);
                usedTextures.Add(s, textureIdx);

                // Check if any of the candidates have all their parents done, in which case they
                // can recycle their texture
                for (var i = 0; i < candidates.Count; ++i) {
                    var cand = candidates[i];
                    bool safeToRemove = cand.Item2.Parents.All(completedNodes.Contains);
                    if (safeToRemove) {
                        textureCache.Push(cand.Item1);
                        candidates.RemoveAt(i);
                    }
                }

                candidates.Add(cur);
            }
            return new GeneratorSequence {NumTextures = nextTextureIdx, Sequence = sequence};
        }
    }

    public class SequenceStep
    {
        public Node Node { get; set; }
        public int DstTextureIdx { get; set; }
        public List<int> InputTextures { get; set; }
    }

    public class GeneratorSequence
    {
        public GeneratorSequence()
        {
            Sequence = new List<SequenceStep>();
        }

        public bool IsPreview { get; set; }

        public string Name { get; set; }
        public Size Size { get; set; }
        public int NumTextures { get; set; }
        public List<SequenceStep> Sequence { get; set; }
    }

}
