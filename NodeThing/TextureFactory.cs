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
        private static extern void renderTexture(IntPtr hwnd, IntPtr key, int width, int height, int numTextures, int finalTexture, 
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
            AddNodeName("Random", 8);
            AddNodeName("Sinwaves", 9);
            AddNodeName("Plasma", 10);

            AddNodeName("Distort", 11);

            AddNodeName("Blur", 12);
        }

        public override Node CreateNode(string name, Point pos)
        {
            if (name != "Sink" && GetNodeId(name) == -1)
                return null;

            var node = new Node { Name = name, Pos = pos };
            var rnd = new Random();

            if (name == "Sink") {
                node.AddInput("Sink", Connection.Type.Texture);
                node.AddProperty("Name", "");
                node.AddProperty("Size", new Size(512, 512));
            }

            if (name == "Solid") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Color", Color.FromArgb(255, 128, 128, 128));
            }

            if (name == "Noise") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale", new Tuple<float, float>(5, 5), new Tuple<float, float>(1, 1), new Tuple<float, float>(25, 25));
                node.AddProperty("Offset", new Tuple<float, float>(0, 0), new Tuple<float, float>(0, 0), new Tuple<float, float>(10, 10));
            }

            if (name == "Circles") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Amount", 10, 1, 40);
                node.AddProperty("Size", 0.1f, 0.001f, 1.0f);
                node.AddProperty("Variance", 1.0f, 0.0f, 2.0f);
                node.AddProperty("Fade", 0.05f, 0.0f, 0.5f);
                node.AddProperty("Inner color", Color.FromArgb(255, 190, 190, 190));
                node.AddProperty("Outer color", Color.FromArgb(255, 90, 90, 90));
                node.AddProperty("Seed", rnd.Next());
            }

            if (name == "Random") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale", 1.0f, 0.0f, 2.0f);
                node.AddProperty("Seed", rnd.Next());
            }

            if (name == "Sinwaves") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale", 0.5f, 0.0f, 2.0f);
                node.AddProperty("Num sin", 3, 1, 10);
                node.AddProperty("Func", 0, 0, 10);
                node.AddProperty("Start amp", 0.2f, 0.0f, 2.0f);
                node.AddProperty("End amp", 0.2f, 0.0f, 2.0f);
                node.AddProperty("Start phase", 0.0f, 0.0f, (float)(2 * Math.PI));
                node.AddProperty("End phase", 0.0f, 0.0f, (float)(2 * Math.PI));
                node.AddProperty("Start freq", 20.0f, 0.1f, 150.0f);
                node.AddProperty("End freq", 40.0f, 0.1f, 150.0f);
            }

            if (name == "Plasma") {
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale", 1.0f, 0.0f, 5.0f);
                node.AddProperty("Monochrome", true);
                node.AddProperty("Depth", 7, 1, 9);
                node.AddProperty("Seed", rnd.Next());
            }


            if (name == "Add" || name == "Sub" || name == "Mul" || name == "Max" || name == "Min") {
                node.AddInput("A", Connection.Type.Texture);
                node.AddInput("B", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Blend", new Tuple<float, float>(1, 1), new Tuple<float, float>(-5, -5), new Tuple<float, float>(5, 5));
            }

            if (name == "Distort") {

                node.AddInput("Source", Connection.Type.Texture);
                node.AddInput("Distort", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Scale", 0.01f, 0.001f, 1.0f);
                node.AddProperty("Channels", 0, 0, 3);
            }

            if (name == "Blur") {
                node.AddInput("Source", Connection.Type.Texture);
                node.SetOutput("Output", Connection.Type.Texture);
                node.AddProperty("Radius", 25.0f, 1.0f, 100.0f);
                node.AddProperty("Direction", 2, 0, 2);
                node.AddProperty("Texture mode", 0, 0, 2);
            }

            return node;
        }


        private bool CodeGen(SequenceStep step, ref List<byte> opCodes)
        {
            var node = step.Node;
            var dstTexture = step.DstTextureIdx;
            var name = node.Name;

            if (name == "Sink") {
            }

            var pp = new ParamPusher { OpCodes = opCodes };

            if (name == "Solid") {
                pp.AddPushUInt32((UInt32)node.GetProperty<Color>("Color").ToArgb());
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Noise") {
                // (dst, scaleX, scaleY, offsetX, offsetY)
                var scale = node.GetProperty<Tuple<float, float>>("Scale");
                var offset = node.GetProperty<Tuple<float, float>>("Offset");
                pp.AddPushFloat32(scale.Item2);
                pp.AddPushFloat32(scale.Item2);
                pp.AddPushFloat32(offset.Item2);
                pp.AddPushFloat32(offset.Item1);
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Add" || name == "Sub" || name == "Mul" || name == "Max" || name == "Min") {
                if (step.InputTextures.Count != 2)
                    return false;

                var srcTexture1 = step.InputTextures[0];
                var srcTexture2 = step.InputTextures[1];
                // (dst, src1, scale 1, src2, scale 2)
                var blend = node.GetProperty<Tuple<float, float>>("Blend");
                pp.AddPushFloat32(blend.Item2);
                pp.AddPushUInt32((UInt32)srcTexture2);
                pp.AddPushFloat32(blend.Item1);
                pp.AddPushUInt32((UInt32)srcTexture1);
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Distort") {
                if (step.InputTextures.Count != 2)
                    return false;

                // void modifier_map_distort(int dstTextureIdx, int srcTextureIdx, int distortTextureIdx, float scale, int channels);
                var srcTexture1 = step.InputTextures[0];
                var srcTexture2 = step.InputTextures[1];

                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Channels"));
                pp.AddPushFloat32(node.GetProperty<float>("Scale"));
                pp.AddPushUInt32((UInt32)srcTexture1);
                pp.AddPushUInt32((UInt32)srcTexture2);
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Blur") {
                // void modifier_blur(int dstTextureIdx, int srcTextureIdx, float radius, TextureMode mode, BlurDirection dir)
                if (step.InputTextures.Count != 1)
                    return false;

                var srcTexture1 = step.InputTextures[0];
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Direction"));
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Texture mode"));
                pp.AddPushFloat32(node.GetProperty<float>("Radius"));
                pp.AddPushUInt32((UInt32)srcTexture1);
                pp.AddPushUInt32((UInt32)dstTexture);
    
            }

            if (name == "Circles")
            {
                // (dst, amount, size, variance, InnerColor, OuterColor, randomSeed)
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Seed"));
                pp.AddPushUInt32((UInt32)node.GetProperty<Color>("Outer color").ToArgb());
                pp.AddPushUInt32((UInt32)node.GetProperty<Color>("Inner color").ToArgb());
                pp.AddPushFloat32(node.GetProperty<float>("Fade"));
                pp.AddPushFloat32(node.GetProperty<float>("Variance"));
                pp.AddPushFloat32(node.GetProperty<float>("Size"));
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Amount"));
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Random") {
                // (dst, scale, seed)
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Seed"));
                pp.AddPushFloat32(node.GetProperty<float>("Scale"));
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Sinwaves") {

                // void source_sinwaves(int dstTexture, float scale, int abs, int numSin, float startAmp, float endAmp, float startPhase, float endPhase, float startFreq, float endFreq);
                pp.AddPushFloat32(node.GetProperty<float>("End freq"));
                pp.AddPushFloat32(node.GetProperty<float>("Start freq"));
                pp.AddPushFloat32(node.GetProperty<float>("End phase"));
                pp.AddPushFloat32(node.GetProperty<float>("Start phase"));
                pp.AddPushFloat32(node.GetProperty<float>("End amp"));
                pp.AddPushFloat32(node.GetProperty<float>("Start amp"));
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Num sin"));
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Func"));
                pp.AddPushFloat32(node.GetProperty<float>("Scale"));
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            if (name == "Plasma") {
                // void source_plasma(int dstTexture, float scale, int monochrome, int depth, randomSeed);

                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Seed"));
                pp.AddPushUInt32((UInt32)node.GetProperty<int>("Depth"));
                var monochrome = node.GetProperty<bool>("Monochrome");
                pp.AddPushUInt32(monochrome ? 1U : 0U);
                pp.AddPushFloat32(node.GetProperty<float>("Scale"));
                pp.AddPushUInt32((UInt32)dstTexture);
            }

            pp.AddFunctionCall(GetNodeId(node.Name));

            // pop stack
            pp.AddPopStack();
            opCodes = pp.OpCodes;

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

        public override void DisplaySequence(GeneratorSequence seq, IntPtr displayHandle, IntPtr key)
        {
            var opCodes = SequenceToOpCodes(seq);
            if (opCodes.Count == 0)
                return;

            var finalTexture = seq.Sequence.Last().DstTextureIdx;
            renderTexture(displayHandle, key, seq.Size.Width, seq.Size.Height, seq.NumTextures, finalTexture, seq.Name, opCodes.Count, opCodes.ToArray());
        }

        public override void GenerateCode(GeneratorSequence seq, string filename)
        {
            var opCodes = SequenceToOpCodes(seq);
            if (opCodes.Count == 0)
                return;

            var finalTexture = seq.Sequence.Last().DstTextureIdx;
            generateCode(seq.Size.Width, seq.Size.Height, seq.NumTextures, finalTexture, seq.Name, opCodes.Count, opCodes.ToArray(), filename);
            
        }

        class ParamPusher
        {
            public void AddPushUInt32(UInt32 value)
            {
                // use 8 bit push if value fits
                if (value < 256) {
                    OpCodes.Add(0x6a);
                    AddUInt8((byte)value);
                } else {
                    OpCodes.Add(0x68);
                    AddUInt32(value);
                }
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

            public void AddPushUInt16(UInt16 value)
            {
                OpCodes.Add(0x66);
                OpCodes.Add(0x68);
                AddUInt16(value);
                _stackFixupSize += 2;
            }

            public void AddUInt8(byte value)
            {
                OpCodes.Add(value);
            }

            public void AddUInt16(UInt16 value)
            {
                OpCodes.Add((byte)((value >> 0) & 0xff));
                OpCodes.Add((byte)((value >> 8) & 0xff));
            }

            public void AddUInt32(UInt32 value)
            {
                OpCodes.Add((byte)((value >> 0) & 0xff));
                OpCodes.Add((byte)((value >> 8) & 0xff));
                OpCodes.Add((byte)((value >> 16) & 0xff));
                OpCodes.Add((byte)((value >> 24) & 0xff));
            }

            public void AddPopStack()
            {
                OpCodes.Add(0x81);
                OpCodes.Add(0xc4);
                AddUInt32((UInt32)_stackFixupSize);
            }

            public void AddFunctionCall(Int32 functionId)
            {
                // generate "call [eax + functionId*4]"
                OpCodes.Add(0xff);
                OpCodes.Add(0x90);
                AddUInt32((UInt32)(functionId * 4));
            }

            public List<byte> OpCodes { get; set; }
            int _stackFixupSize = 0;
        }


    }
}
