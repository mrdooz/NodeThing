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

            _nodeNames.AddRange(new [] {"Add", "Sub", "Max", "Min"});
        }

        public override Node CreateNode(string name, Point pos)
        {
            var node = new Node { Name = name, Pos = pos };

            if (name == "Sink") {
                node.AddInput("Sink", Connection.Type.Texture);
                node.AddProperty("Name", "");
                node.AddProperty("Size", new Size(512, 512));
                return node;
            }

            if (name == "Solid") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Color", Color.FromArgb(255, 128, 128, 128));
                return node;
            }

            if (name == "Noise") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Seed", 10);
                return node;
            }

            if (name == "Add") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale A", 10.0f);
                node.AddProperty("Scale B", 10.0f);
                return node;
            }

            if (name == "Sub") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale A", 10.0f);
                node.AddProperty("Scale B", 10.0f);
                return node;
            }

            if (name == "Max") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale A", 10.0f);
                node.AddProperty("Scale B", 10.0f);
                return node;
            }

            if (name == "Min") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale A", 10.0f);
                node.AddProperty("Scale B", 10.0f);
                return node;
            }

            return null;
        }

        public override void GenerateCode(GenerateSequence seq)
        {
            
        }

    }
}
