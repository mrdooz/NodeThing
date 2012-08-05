using System.Runtime.Serialization;

namespace NodeThing
{
    [DataContract]
    public class Connection
    {
        public Connection()
        {
            Used = false;
            Hovering = false;
            ErrorState = false;
        }

        public bool LegalConnection(Connection con)
        {
            if (con.Node == Node)
                return false;

            if (con.DataType != DataType)
                return false;

            if (con.Direction == Direction)
                return false;

            if (con.Direction == Io.Input && con.Used)
                return false;

            return true;
        }

        public enum Type
        {
            Int,
            Float,
            Geometry,
            Texture,
        }

        public enum Io
        {
            Input,
            Output,
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public Type DataType { get; set; }

        [DataMember]
        public Io Direction { get; set; }

        [DataMember]
        public Node Node { get; set; }

        [DataMember]
        public int Slot { get; set; }

        [DataMember]
        public bool Used { get; set; }

        public bool Hovering { get; set; }
        public bool ErrorState { get; set; }
        public bool Selected { get; set; }
    }
}
