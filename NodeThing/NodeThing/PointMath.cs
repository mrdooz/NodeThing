using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NodeThing
{
    class PointMath
    {
        static public float len(PointF pt)
        {
            return (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
        }

        static public PointF normalize(PointF pt)
        {
            float d = len(pt);
            return new PointF(pt.X / d, pt.Y / d);
        }

        static public float dot(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        static public PointF sub(PointF a, PointF b)
        {
            return new PointF(a.X - b.X, a.Y - b.Y);
        }

    }
}
