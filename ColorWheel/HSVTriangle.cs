// This is a modified version of http://colorwheel.codeplex.com/license
// Kept in a seperate library to avoid dragging LGPL into my BSD stuff..

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ColorWheelNET;

namespace ColorWheel
{
    public class HSVTriangle
    {
        #region Constants

        const int _DISPLAY_BUFFER = 2;
        const float _DEGREES_IN_CIRCLE = 360;

        static double _RADIANS_MULTIPLIER = (Math.PI / ((float)180));
        static double _ANGLE_STEP = 5;
        private static double _DEFAULT_VALUE = 1.0;
        private static double _DEFAULT_SATURATION = 1.0;

        //private static int _WHITE_CLICK_TOLERANCE = 20;

        private static int _WHEEL_COLOR_RING_PERCENT = 13;

        #endregion



        internal static PointF MidPoint(PointF p1, PointF p2)
        {
            return new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
        }

        /// <summary>
        /// Calculate selector color based on where's it at
        /// Like if it's at dark color area, make selector brighter ...etc...
        /// </summary>
        /// <param name="wheel"></param>
        public static void CalculateSelectorColor(CWheel wheel)
        {
            Color color = wheel.Color;
            double h, s, v;
            ColorHelper.HSVFromRGB(color, out h, out s, out v);

            if (color == Color.White ||
               (color.A == Color.White.A &&
               color.R == Color.White.R &&
               color.G == Color.White.G &&
               color.B == Color.White.B) ||
               s < 0.5) {
                wheel.SelectorColor = Color.DarkSlateGray;
                return;
            }

            if (color == Color.Black ||
                (color.A == Color.Black.A &&
                color.R == Color.Black.R &&
                color.G == Color.Black.G &&
                color.B == Color.Black.B) ||
                v < 0.5) {
                wheel.SelectorColor = Color.WhiteSmoke;
                return;
            }

            h = (int)h; // prevent h has a value sometimes like 40.0000000007 which is still > 40 and it cause flickering at the h=40 line

            if (h >= 0 && h <= 40 || (h >= 200 && h <= 360)) {
                wheel.SelectorColor = Color.WhiteSmoke;
            } else if (h > 40 && h < 200) {
                wheel.SelectorColor = Color.DimGray;

                if (v < 0.8) {
                    wheel.SelectorColor = Color.WhiteSmoke;
                } else if (s < 0.8) {
                    wheel.SelectorColor = Color.DimGray;
                }
            }
        }

        /// <summary>
        /// We need to draw a small box around the SelectedColor's position in the wheel
        /// </summary>
        /// <param name="graphics"></param>
        public static void DrawSelector(CWheel wheel, Graphics graphics)
        {
            if (!wheel.ShowSelector) { return; }

            if (wheel.SelectedPoint.IsEmpty) {
                wheel.OnCalculateSelectedPoint(wheel);
                //if we failed to calculate it give up
                if (wheel.SelectedPoint.IsEmpty) { return; }
            }

            using (SolidBrush b = new SolidBrush(wheel.SelectorColor)) {
                Rectangle selRec = new Rectangle(wheel.SelectedPoint.X - _DISPLAY_BUFFER, wheel.SelectedPoint.Y - _DISPLAY_BUFFER, _DISPLAY_BUFFER * 2, _DISPLAY_BUFFER * 2);

                Pen p = new Pen(b);
                p.Width = 2;

                graphics.DrawEllipse(p, selRec);
            }
        }

        /// <summary>
        /// all the Draw Wheel calls go here!
        /// </summary>
        /// <param name="graphics"></param>
        public static void DrawWheel(CWheel wheel, Graphics graphics)
        {
            if (wheel.Regions == null || wheel.Regions.Length == 0 || wheel.Colors == null) { return; }

            //Draw Hue circle
            using (PathGradientBrush b = new PathGradientBrush(wheel.Points, WrapMode.Clamp))
            {
               // b.CenterPoint = wheel.WheelCenter;
                b.CenterColor = Color.White;
                b.SurroundColors = wheel.Colors;

                //Draw the wheel
               // graphics.FillPie(b, wheel.WheelRectangle, 0, 360);
                graphics.FillRegion(b, wheel.Regions[0]);
            }

            //Draw Saturation/Value triangle
            if (wheel.InnerPoints != null && wheel.InnerPoints.Length >= 3 && wheel.WheelPath != null)
            {
                //draw pointer
                if (wheel.InnerPoints.Length > 3)
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        Pen p = new Pen(b, 2f);
                        graphics.DrawLine(p, wheel.WheelCenter, wheel.InnerPoints[3]);
                    }
                }

               // GraphicsPath path = wheel.WheelPath;

                //draw triangle
                //step 1) draw triangle as a solid color
                //step 2) draw black gradient next at one of the far corners going to transparent in the other two
                //step 3) draw white gradient, same as above but at the other far corner
                Color cgroup = wheel.ColorGroup != Color.White ? wheel.ColorGroup : Color.Red;
                using (SolidBrush b = new SolidBrush(cgroup)) 
                {
                    graphics.FillPath(b, wheel.WheelPath);
                }

                //MidPoint(wheel.InnerPoints[2], wheel.InnerPoints[0])
                using (LinearGradientBrush b = new LinearGradientBrush(wheel.InnerPoints[1], wheel.InnerPoints[0], Color.Black, Color.Transparent))
                {
                    graphics.FillPath(b, wheel.WheelPath);
                }


                //MidPoint(wheel.InnerPoints[1], wheel.InnerPoints[0])
                using (LinearGradientBrush b = new LinearGradientBrush(wheel.InnerPoints[2], MidPoint(wheel.InnerPoints[1], wheel.InnerPoints[0]), Color.White, Color.Transparent))
                {
                    graphics.FillPath(b, wheel.WheelPath);
                }

                //draw lines around edges of triangle
                using (SolidBrush b = new SolidBrush(Color.Black))
                {
                    Pen p = new Pen(b, 1f);
                    graphics.DrawPath(p, wheel.WheelPath);
                }


                //double hue, saturation, value;
                //ColorHelper.HSVFromRGB(wheel.Color, out hue, out saturation, out value);


                //double dx = wheel.InnerPoints[2].X - wheel.InnerPoints[1].X;
                //double dy = wheel.InnerPoints[2].Y - wheel.InnerPoints[1].Y;
                //double sideLength = Math.Sqrt(dx * dx + dy * dy);
                //double distanceFromSaturation = sideLength * saturation;
                //double distanceFromValue = sideLength * value;

                //RectangleF valRec = new RectangleF(wheel.InnerPoints[2].X - (float)distanceFromValue, wheel.InnerPoints[2].Y - (float)distanceFromValue, (float)distanceFromValue * 2, (float)distanceFromValue * 2);

                //RectangleF satRec = new RectangleF(wheel.InnerPoints[1].X - (float)distanceFromSaturation, wheel.InnerPoints[1].Y - (float)distanceFromSaturation, (float)distanceFromSaturation * 2, (float)distanceFromSaturation * 2);

                //using (SolidBrush b = new SolidBrush(Color.Blue))
                //{
                //    Pen p = new Pen(b, 1f);
                //    graphics.DrawPie(p, valRec, 0f, 360f);
                //}

                //using (SolidBrush b = new SolidBrush(Color.Green))
                //{
                //    Pen p = new Pen(b, 1f);
                //    graphics.DrawPie(p, satRec, 0f, 360f);
                //}

                //using (SolidBrush b = new SolidBrush(Color.Yellow))
                //{
                //    Pen p = new Pen(b, 1f);
                //    graphics.DrawLine(p, wheel.InnerPoints[1], wheel.SelectedPoint);
                //}
            }
        }

        public static void CalculateColorGroup(CWheel wheel, Point pt)
        {
            wheel.ColorGroup = Color.Empty;

            Rectangle wrec = wheel.WheelRectangle;
            Color color = wheel.Color;

            if (color == Color.White || color.R == color.B && color.B == color.G)
            {
                wheel.ColorGroup = Color.White;
                return;
            }

            if (wheel.Colors == null) { return; }

            if (!wrec.Contains(pt)) { return; }


            float radius = (float)((float)wrec.Width / 2f);

            //Calculate the Distance
            //double dX = pt.X - wheel.WheelCenter.X;
            //double dY = pt.Y - wheel.WheelCenter.Y;
            double dX = wheel.InnerPoints[3].X - wheel.WheelCenter.X;
            double dY = wheel.InnerPoints[3].Y - wheel.WheelCenter.Y;
            double distanceFromCenter = Math.Sqrt((dX * dX) + (dY * dY));


            //if the distance of the clicked point is beyond the radius, then we clicked just past the circle border
            if ((int)distanceFromCenter > radius) { return; }

            //Calculate the angle 
            double angle = Math.Atan2(dY, dX) / _RADIANS_MULTIPLIER;
            double radians = angle * _RADIANS_MULTIPLIER - wheel.SegmentRotation;


            //Let's compensate for the user defined radian rotation
            double trueAngle = radians * 180d / Math.PI;

            if (trueAngle < 0)
            {
                trueAngle = _DEGREES_IN_CIRCLE - trueAngle;
            }

            //Standardize it to a max of 360 degrees
            while (trueAngle > _DEGREES_IN_CIRCLE)
            {
                trueAngle -= _DEGREES_IN_CIRCLE;
            }

            //Calculate the color segment index in our this.Colors array
            int segment = (int)(trueAngle / _ANGLE_STEP);

            //For some reason the color segment isn't in the array
            if (segment < 0 || segment > wheel.Colors.Length) { return; }
            wheel.ColorGroup = wheel.Colors[segment];
        }

        internal static void CalculateTriangle(CWheel wheel)
        {
            double h, s, v;//, gh, gs, gv;

            try
            {
                ColorHelper.HSVFromRGB(wheel.Color, out h, out s, out v);
                h = !double.IsNaN(h) ? h : 0;
            }
            catch
            {
                h = 0;
                s = 0;
                v = 0;
            }

            double angle = (h * _RADIANS_MULTIPLIER - wheel.SegmentRotation) / _RADIANS_MULTIPLIER;
            MakeTriangle(wheel, angle);
        }

        public static void CalculateSelectedPoint(CWheel wheel)
        {
            //if we don't have the triangle already, then lets make it!
            if(wheel.WheelPath == null)
                CalculateTriangle(wheel);

            double h, s, v;
            ColorHelper.HSVFromRGB(wheel.Color, out h, out s, out v);
            h = !double.IsNaN(h) ? h : 0;

            PointF vH = wheel.InnerPoints[0];
            PointF vS = wheel.InnerPoints[2];
            PointF vV = wheel.InnerPoints[1];

            // saturation first, then value
            // this design matches with the picture from wiki

            PointF vStoH = new PointF((vH.X - vS.X) * (float)s, (vH.Y - vS.Y) * (float)s);
            PointF vS2 = new PointF(vS.X + vStoH.X, vS.Y + vStoH.Y); 
            PointF vVtovS2 = new PointF((vS2.X - vV.X) * (float)v, (vS2.Y - vV.Y) * (float)v); 
            PointF final = new PointF(vV.X + vVtovS2.X, vV.Y + vVtovS2.Y); 

            wheel.SelectedPoint = new Point((int)final.X, (int)final.Y);

            if (wheel.SelectorColor.IsEmpty && wheel.OnCalculateSelectorColor != null)
            {
                wheel.OnCalculateSelectorColor(wheel);
            }

            wheel.Invalidate();
        }

        private static Region MakeDonut(CWheel wheel)
        {
            if (wheel.WheelRectangle.IsEmpty) { return null; }
            GraphicsPath p1 = new GraphicsPath();
            p1.AddPie(wheel.WheelRectangle, 0f, _DEGREES_IN_CIRCLE);

            int w = (int)(wheel.WheelRectangle.Width / _WHEEL_COLOR_RING_PERCENT);

            wheel.WheelInnerRectangle = new Rectangle(wheel.WheelRectangle.X + w, wheel.WheelRectangle.Y + w, wheel.WheelRectangle.Width - w - w, wheel.WheelRectangle.Height - w - w);

            GraphicsPath p2 = new GraphicsPath();
            p1.AddPie(wheel.WheelInnerRectangle, 0f, _DEGREES_IN_CIRCLE);

            Region region = new Region(p1);
            region.Exclude(p2);

            return region;
        }


        public static void CalculateLayout(CWheel wheel)
        {
            wheel.Regions = null;
            List<Region> regions = new List<Region>();
            Region region = MakeDonut(wheel);
            if (region == null) { return; }
            regions.Add(region);

            List<PointF> points = new List<PointF>();
            List<Color> colors = new List<Color>();

            Rectangle wRec = wheel.WheelRectangle;
            float radius = (float)(wRec.Width / 2);

            Color lastColor = Color.White;


            for (double angle = 0; angle < _DEGREES_IN_CIRCLE; angle += _ANGLE_STEP)
            {
                double radians = angle * _RADIANS_MULTIPLIER - wheel.SegmentRotation;
                points.Add(new PointF((float)(wheel.WheelCenter.X + (Math.Cos(radians) * radius)), (float)(wheel.WheelCenter.Y - Math.Sin(radians) * radius)));

                try
                {
                    lastColor = ColorHelper.ColorFromHSV(angle, _DEFAULT_SATURATION, _DEFAULT_VALUE);
                }
                finally
                {
                    colors.Add(lastColor);
                }
            }

            wheel.Points = points.ToArray();
            wheel.Colors = colors.ToArray();


            wheel.Regions = regions.ToArray();

            //if (wheel.ColorGroup.IsEmpty)
            //{
            //    wheel.ColorGroup = Color.Red;
            //}

            //now it should be safe to create the triangle
            CalculateTriangle(wheel);

            if (wheel.SelectedPoint.IsEmpty && wheel.OnCalculateSelectedPoint != null)
            {
                wheel.OnCalculateSelectedPoint(wheel);
            }

            if (wheel.SelectorColor.IsEmpty && wheel.OnCalculateSelectorColor != null)
            {
                wheel.OnCalculateSelectorColor(wheel);
            }
        }

        private static void MakeTriangle(CWheel wheel, double startAngle)
        {
            //Something wrong with my adjusting for the startAngle
          //  startAngle = startAngle + 190f; //as a temporary hack that partially works with the default rotation...

            if (wheel.WheelInnerRectangle == null || wheel.WheelCenter.IsEmpty) 
            {
                wheel.InnerPoints = null;
                return; 
            }

            float radius = wheel.WheelInnerRectangle.Width / 2f;

            List<PointF> points = new List<PointF>();
            double radians = startAngle * _RADIANS_MULTIPLIER; //compensate for the segment rotation
            points.Add(CalculatePoint(radians, radius, radius, wheel.WheelCenter));
            radians = (120f + startAngle) * _RADIANS_MULTIPLIER;
            points.Add(CalculatePoint(radians, radius, radius, wheel.WheelCenter));
            radians = (240f + startAngle) * _RADIANS_MULTIPLIER;
            points.Add(CalculatePoint(radians, radius,radius,  wheel.WheelCenter));


            //pointer line
            radians = startAngle * _RADIANS_MULTIPLIER;
            points.Add(CalculatePoint(radians, radius, radius + (wheel.WheelRectangle.Width / (double)_WHEEL_COLOR_RING_PERCENT), wheel.WheelCenter));

            wheel.InnerPoints = points.ToArray();

            GraphicsPath path = new GraphicsPath();
            path.AddLines(new PointF[] { wheel.InnerPoints[0], wheel.InnerPoints[1], wheel.InnerPoints[2] });
            path.CloseAllFigures();

            wheel.WheelPath = path;
        }

        private static PointF CalculatePoint(double radians, double radius, double distanceFromCenter, PointF wheelCenter)
        { 
            double dX = distanceFromCenter * Math.Cos(radians);
            double dY = distanceFromCenter * Math.Sin(radians);

            return new PointF((int)wheelCenter.X + (int)dX, (int)wheelCenter.Y - (int)dY);
        }

        public static void ColorHitTest(CWheel wheel, Point pt)
        {

            if (wheel.Regions == null || wheel.Regions.Length == 0) { return; }

            //ColorGroupHitTest(wheel, pt);
            if (wheel.WheelMouseDown || wheel.Regions[0].IsVisible(pt) && !wheel.WheelMouseDown2)
            {
                ColorGroupHitTest(wheel, pt);
            }
            else
            {
                ColorSaturationValueHitTest(wheel, pt);
            }
        }

        private static void ColorSaturationValueHitTest(CWheel wheel, Point pt)
        {
            if (wheel.InnerPoints == null || wheel.InnerPoints.Length < 3 || wheel.WheelPath == null) { return; }

           // GraphicsPath path = wheel.WheelPath;
            if (wheel.WheelPath.IsVisible(pt))
            {
                wheel.WheelMouseDown2 = true;

                double h, s, v;

                ColorHelper.HSVFromRGB(wheel.ColorGroup, out h, out s, out v);

                h = !double.IsNaN(h) ? h : 0;

                PointF vH = wheel.InnerPoints[0];
                PointF vV = wheel.InnerPoints[1];
                PointF vS = wheel.InnerPoints[2];

                PointF vVtoPoint = new PointF(pt.X - vV.X, pt.Y - vV.Y);
                PointF vVtovS = new PointF(vS.X - vV.X, vS.Y - vV.Y);

                // a *dot* b = ||a|| ||b|| cos(o)
                // gonna find the angle between the 2 vectors: vV-> clicked point and vV -> vS
                // then ratio it against PI / 3 (60 degree), I believed the ratio should be the same with the ratio on the vector vS -> vH 

                double dotproduct = vVtoPoint.X * vVtovS.X + vVtoPoint.Y * vVtovS.Y;
                double vVtoPointLength = Math.Sqrt(vVtoPoint.X * vVtoPoint.X + vVtoPoint.Y * vVtoPoint.Y);
                double vVtovSLength = Math.Sqrt(vVtovS.X * vVtovS.X + vVtovS.Y * vVtovS.Y);
                double angle = Math.Acos(dotproduct / (vVtoPointLength * vVtovSLength));
                s = angle / (Math.PI / 3); // use this ratio for saturation
                s = s <= 1.0 ?
                    s >= 0 ? s : 0
                    : 1.0;
                            
                            


                PointF vStovH = new PointF(vH.X - vS.X, vH.Y - vS.Y);
                PointF vStovH2 = new PointF(vStovH.X * (float)s, vStovH.Y * (float)s); // apply scalar to get new vector
                PointF vVtovH2 = new PointF(vVtovS.X + vStovH2.X, vVtovS.Y + vStovH2.Y);
                double vVtovH2Length = Math.Sqrt(vVtovH2.X * vVtovH2.X + vVtovH2.Y * vVtovH2.Y);
                v = vVtoPointLength / vVtovH2Length; // ratio for value

                v = v <= 1.0 ?
                     v >= 0 ? v : 0
                    : 1.0;

                wheel.SetSelectedColor(ColorHelper.ColorFromHSV(h, s, v), pt, wheel.ColorGroup);
                wheel.Invalidate();
            }
        }

        private static void ColorGroupHitTest(CWheel wheel, Point pt)
        {
            wheel.WheelMouseDown = true;

            Rectangle wrec = wheel.WheelRectangle;
            Color color = wheel.Color;

            if (wheel.Colors == null) { return; }

            if (!wrec.Contains(pt)) { return; }


            float radius = (float)((float)wrec.Width / 2f);

            //Calculate the Distance
            double dX = pt.X - wheel.WheelCenter.X;
            double dY = pt.Y - wheel.WheelCenter.Y;

            //Calculate the angle 
            double angle = Math.Atan2(dY, dX) / _RADIANS_MULTIPLIER;
            double radians = angle * _RADIANS_MULTIPLIER - wheel.SegmentRotation;


            //Let's compensate for the user defined radian rotation
            double trueAngle = radians * 180d / Math.PI; ;

      //      if (trueAngle < 0)
      //      {
                trueAngle = _DEGREES_IN_CIRCLE - trueAngle;
       //     }

            //Standardize it to a max of 360 degrees
            while (trueAngle > _DEGREES_IN_CIRCLE)
            {
                trueAngle -= _DEGREES_IN_CIRCLE;
            }

            //Calculate the color segment index in our this.Colors array
            int segment = (int)(trueAngle / _ANGLE_STEP);

            //For some reason the color segment isn't in the array
            if (segment < 0 || segment > wheel.Colors.Length) { return; }

            double saturation = _DEFAULT_SATURATION;
            double value = _DEFAULT_VALUE;
            double hue = 0d;

            //let's maintain the current positioning in the triangle when we change color groups 
            //this is consistant with gimp and coreldraw
            ColorHelper.HSVFromRGB(wheel.Color, out hue, out saturation, out value);
            hue = trueAngle;

            //saturation = saturation > 0 ? saturation : 0.1;
            //value = value > 0 ? value : 0.1;
            //This is our true color.
            color = ColorHelper.ColorFromHSV(hue, saturation, value); //this.Colors[segment];


            MakeTriangle(wheel, _DEGREES_IN_CIRCLE - angle);

            //For now actually set it as the color, in the future we will just assign the colorgroup
            wheel.SetSelectedColor(color, Point.Empty, wheel.Colors[segment]);
        }

    }

}
