using System;
using System.Collections.Generic;
using System.Drawing;

namespace NodeThing
{
    public class NodeFactory
    {
        public NodeFactory()
        {
            _nodeNames.Add("Cube");

            _nodeNames.Add("Array");

            _nodeNames.Add("Sink");
        }

        public Node CreateNode(string name, Point pos)
        {
            var node = new Node { Name = name, Pos = pos };
            if (name == "Cube") {
                node.Output = new Connection { Name = "Output", DataType = Connection.Type.Geometry, Direction = Connection.Io.Output, Node = node, Slot = 0 };
                return node;
            }

            if (name == "Array") {
                node.AddInput("Input1", Connection.Type.Geometry);
                node.AddInput("Input3-Input3-Input3", Connection.Type.Geometry);
                node.AddInput("Input2", Connection.Type.Geometry);

                node.SetOutput("Output", Connection.Type.Geometry);

                return node;
            }

            if (name == "Sink") {
                node.AddInput("Sink", Connection.Type.Geometry);
                return node;
            }

            return null;
        }
        public IEnumerable<String> NodeNames()
        {
            return _nodeNames;
        }

        List<String> _nodeNames = new List<string>();

    }
}
