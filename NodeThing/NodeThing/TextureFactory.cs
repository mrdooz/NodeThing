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
            AddNodeName("Solid", 0);
            AddNodeName("Noise", 1);

            AddNodeName("Add", 2);
            AddNodeName("Sub", 3);
            AddNodeName("Max", 4);
            AddNodeName("Min", 5);
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

        private void AddPushInt32(Int32 value, ref List<char> opCodes)
        {
            opCodes.Add((char)0x68);
            AddInt32(value, ref opCodes);
        }

        private void AddPushInt16(Int16 value, ref List<char> opCodes)
        {
            opCodes.Add((char)0x68);
            AddInt16(value, ref opCodes);
        }

        private void AddInt32(Int32 value, ref List<char> opCodes)
        {
            opCodes.Add((char)((value >>  0) & 0xff));
            opCodes.Add((char)((value >>  8) & 0xff));
            opCodes.Add((char)((value >> 16) & 0xff));
            opCodes.Add((char)((value >> 24) & 0xff));
        }

        private void AddInt16(Int16 value, ref List<char> opCodes)
        {
            opCodes.Add((char)((value >> 0) & 0xff));
            opCodes.Add((char)((value >> 8) & 0xff));
        }

        private void AddPopStack(Int32 amount, ref List<char> opCodes)
        {
            opCodes.Add((char)0x81);
            opCodes.Add((char)0xc4);
            AddInt32(amount, ref opCodes);
        }

        private void CodeGen(int dstTexture, Node node, ref List<char> opCodes)
        {
            var name = node.Name;

            if (name == "Sink") {
            }

            if (name == "Solid") {
                AddPushInt32(((Color)node.Properties["Color"].Value).ToArgb(), ref opCodes);
                AddPushInt32(dstTexture, ref opCodes);

                // generate "call [eax + nodeId*4]"
                opCodes.Add((char)0xff);
                opCodes.Add((char)0x90);
                AddInt32(GetNodeId(node.Name)*4, ref opCodes);

                // pop stack
                AddPopStack(8, ref opCodes);
            }

            if (name == "Noise") {
            }

            if (name == "Add") {
            }

            if (name == "Sub") {
            }

            if (name == "Max") {
            }

            if (name == "Min") {
            }
            
        }

        public override List<char> GenerateCode(GenerateSequence seq)
        {
            var res = new List<char>();

            foreach (var s in seq.Sequence) {
                int dstTexture = s.Item1;
                var node = s.Item2;
                CodeGen(dstTexture, node, ref res);
            }

            return res;
        }

    }
}
