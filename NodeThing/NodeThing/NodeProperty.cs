using System;
using System.Drawing;
using System.Runtime.Serialization;

namespace NodeThing
{
    public enum PropertyType
    {
        Float,
        Float2,
        Int,
        Int2,
        Size,
        Color,

        String,
    }

    [DataContract]
    public class NodePropertyBase
    {
        [DataMember]
        public bool IsBounded { get; set; }

        [DataMember]
        public PropertyType PropertyType { get; protected set; }
    }

    [DataContract]
    public class NodeProperty<T> : NodePropertyBase
    {
        [DataMember]
        public T Value { get; set; }

        [DataMember]
        public T Min { get; set; }

        [DataMember]
        public T Max { get; set; }

        private void SetPropertyType(T value)
        {
            var t = typeof(T);
            if (t == typeof(int)) {
                PropertyType = PropertyType.Int;
            } else if (t == typeof(float)) {
                PropertyType = PropertyType.Float;
            } else if (t == typeof(Tuple<float, float>)) {
                PropertyType = PropertyType.Float2;
            } else if (t == typeof(Tuple<int, int>)) {
                PropertyType = PropertyType.Int2;
            } else if (t == typeof(Size)) {
                PropertyType = PropertyType.Size;
            } else if (t == typeof(Color)) {
                PropertyType = PropertyType.Color;
            } else if (t == typeof(String)) {
                PropertyType = PropertyType.String;
            } else {
                throw new Exception("Unhandled property type: " + value.GetType());
            }
        }

        public NodeProperty(T value)
        {
            Value = value;
            IsBounded = false;
            SetPropertyType(value);
        }

        public NodeProperty(T value, T minValue, T maxValue)
        {
            Value = value;
            Min = minValue;
            Max = maxValue;
            IsBounded = true;
            SetPropertyType(value);
        }


        public override string ToString()
        {
            return Value.ToString();
        }

    }
}