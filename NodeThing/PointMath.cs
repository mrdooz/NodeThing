using System;
using System.Drawing;

namespace NodeThing
{
    class PointMath
    {
        static public float Len(PointF pt)
        {
            return (float)Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
        }

        static public PointF Normalize(PointF pt)
        {
            float d = Len(pt);
            return new PointF(pt.X / d, pt.Y / d);
        }

        static public float Dot(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        static public PointF Sub(PointF a, PointF b)
        {
            return new PointF(a.X - b.X, a.Y - b.Y);
        }

        static public Point Sub(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        static public Point Add(Point a, Point b)
        {
            return new Point(a.X + b.X, a.Y + b.Y);
        }

        static public Point Min(Point a, Point b)
        {
            return new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        }

        static public Point Max(Point a, Point b)
        {
            return new Point(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }

        static public Size Diff(Point a, Point b)
        {
            return new Size(a.X - b.X, a.Y - b.Y);
        }

    }
}
