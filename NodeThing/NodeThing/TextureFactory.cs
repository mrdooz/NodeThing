﻿using System;
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

            return null;
        }
    }
}
