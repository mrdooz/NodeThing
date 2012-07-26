using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    public class NodeFactory
    {
        public NodeFactory()
        {
            _nodeNames.Add("Cube");

            _nodeNames.Add("Array");
        }

        public Node createNode(string name, Point pos)
        {
            var node = new Node() { name = name, pos = pos };
            if (name == "Cube")
            {
                node.output = new Connection { name = "Output", type = Connection.Type.kGeometry };
                return node;
            }

            if (name == "Array")
            {
                node.inputs.Add(new Connection { name = "Input", type = Connection.Type.kGeometry });
                return node;
            }

            return null;
        }
        public IEnumerable<String> nodeNames()
        {
            return _nodeNames;
        }

        List<String> _nodeNames = new List<string>();

    }
}
