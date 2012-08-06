using System;

namespace NodeThing
{
    class Utils
    {
        public static T Clamp<T>(T value, T minValue, T maxValue) where T : IComparable
        {
            if (value.CompareTo(minValue) < 0)
                value = minValue;
            if (value.CompareTo(maxValue) > 0)
                value = maxValue;
            return value;
        }

    }
}
