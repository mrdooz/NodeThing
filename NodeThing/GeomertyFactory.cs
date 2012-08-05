using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NodeThing
{
/*
    public class GeometryFactory : NodeFactory
    {
        public GeometryFactory(CompletedCallback callback) : base(callback)
        {
            AddNodeName("Cube", 0);
            AddNodeName("Array", 1);
        }

        public override Node CreateNode(string name, Point pos)
        {
            var node = new Node { Name = name, Pos = pos };

            if (name == "Sink") {
                node.AddInput("Sink", Connection.Type.Geometry);
                return node;
            }

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

            return null;
        }

        public override void DisplaySequence(GeneratorSequence seq, IntPtr displayHandle)
        {
            throw new NotImplementedException();
        }
    }
 */
}
