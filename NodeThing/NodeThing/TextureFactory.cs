using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NodeThing
{
    class TextureFactory : NodeFactory
    {

        public TextureFactory()
        {
            _nodeNames.Add("Solid");
            _nodeNames.Add("Noise");
        }

        public override Node CreateNode(string name, Point pos)
        {
            var node = new Node { Name = name, Pos = pos };

            if (name == "Sink") {
                node.AddInput("Sink", Connection.Type.Texture);
                return node;
            }

            if (name == "Solid") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Size", new Size(512, 512));
                node.AddProperty("Color", Color.FromArgb(255, 128, 128, 128));
                return node;
            }

            if (name == "Noise") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Size", new Size(512, 512));
                node.AddProperty("Seed", 10);
                return node;
            }

            return null;
        }
    }
}
