using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeThing
{
    public class Connection
    {
        public Connection()
        {
            Used = false;
        }

        public enum Type
        {
            Int,
            Float,
            Geometry,
        }

        public enum Io
        {
            Input,
            Output,
        }

        public string Name { get; set; }
        public Type DataType { get; set; }
        public Io Direction { get; set; }
        public Node Node { get; set; }
        public int Slot { get; set; }
        public bool Used { get; set; }

    }
}
