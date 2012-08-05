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

            AddNodeName("Circles", 7);
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

            if (name == "Circles")
            {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Amount", 5);
                node.AddProperty("Size", 50.0f);
                node.AddProperty("Variance", 3.0f);
                node.AddProperty("Inner color", Color.FromArgb(255, 190, 190, 190));
                node.AddProperty("Outer color", Color.FromArgb(255, 90, 90, 90));
                return node;
            }

            return null;
        }

        class ParamPusher
        {
            public void AddPushInt32(Int32 value)
            {
                OpCodes.Add(0x68);
                AddInt32(value);
                _stackFixupSize += 4;
            }

            public void AddPushFloat32(Single value)
            {
                OpCodes.Add(0x68);
                var bytes = BitConverter.GetBytes(value);
                foreach (var b in bytes)
                    OpCodes.Add(b);
                _stackFixupSize += 4;
            }

            public void AddPushInt16(Int16 value)
            {
                OpCodes.Add(0x66);
                OpCodes.Add(0x68);
                AddInt16(value);
                _stackFixupSize += 2;
            }

            public void AddInt32(Int32 value)
            {
                OpCodes.Add((byte)((value >> 0) & 0xff));
                OpCodes.Add((byte)((value >> 8) & 0xff));
                OpCodes.Add((byte)((value >> 16) & 0xff));
                OpCodes.Add((byte)((value >> 24) & 0xff));
            }

            public void AddInt16(Int16 value)
            {
                OpCodes.Add((byte)((value >> 0) & 0xff));
                OpCodes.Add((byte)((value >> 8) & 0xff));
            }

            public void AddPopStack()
            {
                OpCodes.Add(0x81);
                OpCodes.Add(0xc4);
                AddInt32(_stackFixupSize);
            }

            public void AddFunctionCall(Int32 functionId)
            {
                // generate "call [eax + functionId*4]"
                OpCodes.Add(0xff);
                OpCodes.Add(0x90);
                AddInt32(functionId * 4);
            }

            public List<byte> OpCodes { get; set; }
            int _stackFixupSize = 0;
        }

        private bool CodeGen(SequenceStep step, ref List<byte> opCodes)
        {
            var node = step.Node;
            var dstTexture = step.DstTextureIdx;
            var name = node.Name;

            if (name == "Sink") {
            }

            if (name == "Solid") {
                var pp = new ParamPusher { OpCodes = opCodes };
                pp.AddPushInt32(node.GetProperty<Color>("Color").ToArgb());
                pp.AddPushInt32(dstTexture);

                pp.AddFunctionCall(GetNodeId(node.Name));

                // pop stack
                pp.AddPopStack();
                opCodes = pp.OpCodes;
            }

            if (name == "Noise") {
                // (dst, scaleX, scaleY, offsetX, offsetY)
                var pp = new ParamPusher { OpCodes = opCodes };
                var scale = node.GetProperty<Tuple<float, float>>("Scale");
                var offset = node.GetProperty<Tuple<float, float>>("Offset");
                pp.AddPushFloat32(scale.Item2);
                pp.AddPushFloat32(scale.Item2);
                pp.AddPushFloat32(offset.Item2);
                pp.AddPushFloat32(offset.Item1);
                pp.AddPushInt32(dstTexture);

                pp.AddFunctionCall(GetNodeId(node.Name));

                // pop stack
                pp.AddPopStack();
                opCodes = pp.OpCodes;
            }

            if (name == "Add" || name == "Sub" || name == "Mul" || name == "Max" || name == "Min") {
                if (step.InputTextures.Count != 2)
                    return false;

                var srcTexture1 = step.InputTextures[0];
                var srcTexture2 = step.InputTextures[1];
                // (dst, src1, scale 1, src2, scale 2)
                var pp = new ParamPusher { OpCodes = opCodes };
                var blend = node.GetProperty<Tuple<float, float>>("Blend");
                pp.AddPushFloat32(blend.Item2);
                pp.AddPushInt32(srcTexture2);
                pp.AddPushFloat32(blend.Item1);
                pp.AddPushInt32(srcTexture1);
                pp.AddPushInt32(dstTexture);

                pp.AddFunctionCall(GetNodeId(node.Name));

                pp.AddPopStack();
                opCodes = pp.OpCodes;
            }

            if (name == "Circles")
            {
                var pp = new ParamPusher { OpCodes = opCodes };
                // (dst, amount, size, variance, InnerColor, OuterColor)
                pp.AddPushInt32(node.GetProperty<Color>("Outer color").ToArgb());
                pp.AddPushInt32(node.GetProperty<Color>("Inner color").ToArgb());
                pp.AddPushFloat32(node.GetProperty<float>("Variance"));
                pp.AddPushFloat32(node.GetProperty<float>("Size"));
                pp.AddPushInt32(node.GetProperty<int>("Amount"));
                pp.AddPushInt32(dstTexture);

                pp.AddFunctionCall(GetNodeId(node.Name));

                pp.AddPopStack();
                opCodes = pp.OpCodes;
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
