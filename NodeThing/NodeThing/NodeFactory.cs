using System;
using System.Collections.Generic;
using System.Drawing;

namespace NodeThing
{
    public abstract class NodeFactory
    {
        protected NodeFactory()
        {
            AddNodeName("Sink", -1);
        }

        public abstract Node CreateNode(string name, Point pos);
        public abstract List<char> GenerateCode(GenerateSequence seq);

        public List<String> NodeNames()
        {
            var res = new List<String>();

            foreach (var n in _nodeState) {
                if (!n.Value.Deprecated)
                    res.Add(n.Key);
            }
            return res;
        }

        protected void AddNodeName(string name, int id, bool deprecated = false)
        {
            _nodeNames.Add(name);
            _nodeState.Add(name, new NodeState { Id = id, Deprecated = deprecated });
        }

        protected int GetNodeId(string name)
        {
            NodeState state;
            if (_nodeState.TryGetValue(name, out state))
                return state.Id;
            return -1;
        }

        class NodeState
        {
            public int Id { get; set; }
            public bool Deprecated { get; set; }
        }

        private List<String> _nodeNames = new List<string>();
        private Dictionary<string, NodeState> _nodeState = new Dictionary<string, NodeState>();
    }
}
