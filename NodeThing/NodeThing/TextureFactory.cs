using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NodeThing
{
    class TextureFactory : NodeFactory
    {
        [DllImport("TextureLib.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool initTextureLib(MulticastDelegate callback);

        [DllImport("TextureLib.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool closeTextureLib();

        [DllImport("TextureLib.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void renderTexture(IntPtr hwnd, int width, int height, int numTextures, int finalTexture, 
            [MarshalAs(UnmanagedType.LPStr)] String name, int opCodeLen, byte[] opCodes);


        [DllImport("TextureLib.dll", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern void generateCode(int width, int height, int numTextures, int finalTexture, 
            [MarshalAs(UnmanagedType.LPStr)]String name, int opCodeLen, byte[] opCodes, [MarshalAs(UnmanagedType.LPStr)] String filename);


        public TextureFactory(CompletedCallback callback) : base(callback)
        {
            initTextureLib(_completedCallback);

            AddNodeName("Solid", 0);
            AddNodeName("Noise", 1);

            AddNodeName("Add", 2);
            AddNodeName("Sub", 3);
            AddNodeName("Max", 4);
            AddNodeName("Min", 5);

            AddNodeName("Mul", 6);
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
                node.AddProperty("Scale", new Tuple<float, float>(5, 5), new Tuple<float, float>(1, 1), new Tuple<float, float>(25, 25));
                node.AddProperty("Offset", new Tuple<float, float>(0, 0), new Tuple<float, float>(0, 0), new Tuple<float, float>(10, 10));
                return node;
            }

            if (name == "Add" || name == "Sub" || name == "Mul" || name == "Max" || name == "Min") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Blend", new Tuple<float, float>(1, 1), new Tuple<float, float>(-5, -5), new Tuple<float, float>(5, 5));
                return node;
            }

            return null;
        }

        private void AddPushInt32(Int32 value, ref List<byte> opCodes)
        {
            opCodes.Add(0x68);
            AddInt32(value, ref opCodes);
        }

        private void AddPushFloat32(Single value, ref List<byte> opCodes)
        {
            opCodes.Add(0x68);
            var bytes = BitConverter.GetBytes(value);
            foreach (var b in bytes)
                opCodes.Add(b);
        }

        private void AddPushInt16(Int16 value, ref List<byte> opCodes)
        {
            opCodes.Add(0x66);
            opCodes.Add(0x68);
            AddInt16(value, ref opCodes);
        }


        private void AddInt32(Int32 value, ref List<byte> opCodes)
        {
            opCodes.Add((byte)((value >>  0) & 0xff));
            opCodes.Add((byte)((value >>  8) & 0xff));
            opCodes.Add((byte)((value >> 16) & 0xff));
            opCodes.Add((byte)((value >> 24) & 0xff));
        }

        private void AddInt16(Int16 value, ref List<byte> opCodes)
        {
            opCodes.Add((byte)((value >> 0) & 0xff));
            opCodes.Add((byte)((value >> 8) & 0xff));
        }

        private void AddPopStack(Int32 amount, ref List<byte> opCodes)
        {
            opCodes.Add(0x81);
            opCodes.Add(0xc4);
            AddInt32(amount, ref opCodes);
        }

        private void AddFunctionCall(Int32 functionId, ref List<byte> opCodes)
        {
            // generate "call [eax + functionId*4]"
            opCodes.Add(0xff);
            opCodes.Add(0x90);
            AddInt32(functionId * 4, ref opCodes);
        }

        private bool CodeGen(SequenceStep step, ref List<byte> opCodes)
        {
            var node = step.Node;
            var dstTexture = step.DstTextureIdx;
            var name = node.Name;

            if (name == "Sink") {
            }

            if (name == "Solid") {
                AddPushInt32(node.GetProperty<Color>("Color").ToArgb(), ref opCodes);
                AddPushInt32(dstTexture, ref opCodes);

                AddFunctionCall(GetNodeId(node.Name), ref opCodes);

                // pop stack
                AddPopStack(2*4, ref opCodes);
            }

            if (name == "Noise") {
                // (dst, scaleX, scaleY)
                var scale = node.GetProperty<Tuple<float, float>>("Scale");
                AddPushFloat32(scale.Item2, ref opCodes);
                AddPushFloat32(scale.Item1, ref opCodes);
                AddPushInt32(dstTexture, ref opCodes);

                AddFunctionCall(GetNodeId(node.Name), ref opCodes);

                // pop stack
                AddPopStack(3 * 4, ref opCodes);

            }

            if (name == "Add" || name == "Sub" || name == "Mul" || name == "Max" || name == "Min") {
                if (step.InputTextures.Count != 2)
                    return false;

                var srcTexture1 = step.InputTextures[0];
                var srcTexture2 = step.InputTextures[1];
                // (dst, src1, scale 1, src2, scale 2)
                var blend = node.GetProperty<Tuple<float, float>>("Blend");
                AddPushFloat32(blend.Item2, ref opCodes);
                AddPushInt32(srcTexture2, ref opCodes);
                AddPushFloat32(blend.Item1, ref opCodes);
                AddPushInt32(srcTexture1, ref opCodes);
                AddPushInt32(dstTexture, ref opCodes);

                AddFunctionCall(GetNodeId(node.Name), ref opCodes);

                AddPopStack(5*4, ref opCodes);
            }

            return true;
        }

        List<byte> SequenceToOpCodes(GeneratorSequence seq)
        {
            var opCodes = new List<byte>();

            foreach (var s in seq.Sequence) {
                // push eax
                opCodes.Add(0x50);
                if (!CodeGen(s, ref opCodes)) {
                    opCodes.Clear();
                    break;
                }
                // pop eax
                opCodes.Add(0x58);
            }
            return opCodes;
        }

        public override void DisplaySequence(GeneratorSequence seq, IntPtr displayHandle)
        {
            var opCodes = SequenceToOpCodes(seq);
            if (opCodes.Count == 0)
                return;

            var finalTexture = seq.Sequence.Last().DstTextureIdx;
            renderTexture(displayHandle, seq.Size.Width, seq.Size.Height, seq.NumTextures, finalTexture, seq.Name, opCodes.Count, opCodes.ToArray());
        }

        public override void GenerateCode(GeneratorSequence seq, string filename)
        {
            var opCodes = SequenceToOpCodes(seq);
            if (opCodes.Count == 0)
                return;

            var finalTexture = seq.Sequence.Last().DstTextureIdx;
            generateCode(seq.Size.Width, seq.Size.Height, seq.NumTextures, finalTexture, seq.Name, opCodes.Count, opCodes.ToArray(), filename);
            
        }

    }
}
