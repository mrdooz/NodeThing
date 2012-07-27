using System;
using System.Collections.Generic;
using System.Drawing;

namespace NodeThing
{
    public abstract class NodeFactory
    {
        protected NodeFactory()
        {
            _nodeNames.Add("Sink");
        }

        abstract public Node CreateNode(string name, Point pos);

        public IEnumerable<String> NodeNames()
        {
            return _nodeNames;
        }

        protected List<String> _nodeNames = new List<string>();
    }
}
