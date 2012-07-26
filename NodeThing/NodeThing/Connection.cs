using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeThing
{
    public class Connection
    {
        public enum Type
        {
            kInt,
            kFloat,
            kGeometry,
        }

        public enum Io
        {
            kInput,
            kOutput,
        }

        public string name { get; set; }
        public Type type { get; set; }
        public Io io { get; set; }
    }
}
