using System;
using System.Collections.Generic;
using System.Drawing;

namespace NodeThing
{
    [Serializable()]
    public class Node
    {
        public Node()
        {
            Inputs = new List<Connection>();
        }

        private int CalcWidth(Graphics g)
        {
            const int defaultWidth = 100;
            var maxInput = 0;
            foreach (var input in Inputs) {
                var bounds = g.MeasureString(input.Name, _font);
                maxInput = Math.Max(maxInput, (int)(bounds.Width + 0.5));
            }

            if (Output != null) {
                var bounds = g.MeasureString(Output.Name, _font);
                maxInput += Padding + (int)bounds.Width + 4 * ConnectionRadius;
            } else {
                maxInput += 2 * ConnectionRadius;
            }

            return Math.Max(defaultWidth, maxInput);
        }

        public void AddInput(string name, Connection.Type type)
        {
            Inputs.Add(new Connection { Name = name, DataType = type, Direction = Connection.Io.Input, Node = this, Slot = Inputs.Count });
        }

        public void SetOutput(string name, Connection.Type type)
        {
            Output = new Connection { Name = name, DataType = type, Direction = Connection.Io.Output, Node = this, Slot = 0 };
        }

        public Connection PointInsideConnection(Point pt)
        {
            var x = Pos.X;
            var y = Pos.Y;

            for (var i = 0; i < Inputs.Count; ++i) {
                var topY = HeaderHeight + Padding + i * ConnectionHeight;
                var bottomY = topY + ConnectionHeight;
                var middleY = (topY + bottomY) / 2;

                var dx = pt.X - x;
                var dy = pt.Y - (y + middleY);
                if (Math.Sqrt(dx * dx + dy * dy) < ConnectionRadius)
                    return Inputs[i];
            }

            if (Output != null) {
                var dx = pt.X - (x + _width);
                var dy = pt.Y - (y + HeaderHeight + _height / 2);
                if (Math.Sqrt(dx * dx + dy * dy) < ConnectionRadius)
                    return Output;
            }

            return null;
        }

        public Tuple<bool, Point> ConnectionPos(Connection.Io io, int slot)
        {
            var invalidPos = new Tuple<bool, Point>(false, new Point(0, 0));

            if (io == Connection.Io.Output) {
                if (Output == null)
                    return invalidPos;
                return new Tuple<bool, Point>(true, new Point(Pos.X, Pos.Y + HeaderHeight + _height/2));
            } else {
                if (slot >= Inputs.Count)
                    return invalidPos;
                return new Tuple<bool, Point>(true, new Point(Pos.X, Pos.Y + HeaderHeight + Padding + ConnectionHeight/2 + slot * ConnectionHeight));
            }
        }

        public bool PointInsideBody(Point pt)
        {
            var x = Pos.X;
            var y = Pos.Y;

            var dx = pt.X - (x + ConnectionRadius);
            var dy = pt.Y - (y + ConnectionRadius);
            var leftDist = Math.Sqrt(dx * dx + dy * dy);

            dx = pt.X - (x + _width - ConnectionRadius);
            var rightDist = Math.Sqrt(dx * dx + dy * dy);

            var middle = new Rectangle(x + ConnectionRadius, y, _width - 2 * ConnectionRadius, HeaderHeight);
            var body = new Rectangle(Pos.X, Pos.Y + HeaderHeight, _width, _height);

            return body.Contains(pt) || middle.Contains(pt) || leftDist < ConnectionRadius || rightDist < ConnectionRadius;
        }

        public void Render(Graphics g)
        {
            g.ResetTransform();
            g.TranslateTransform(Pos.X, Pos.Y);

            if (_needsUpdate) {
                _needsUpdate = false;
                var numSlots = Math.Max(Inputs.Count, Output != null ? 1 : 0);
                _height = 2 * Padding + ConnectionHeight * numSlots;
                _width = CalcWidth(g);
            }

            // Draw header
            var brush = new SolidBrush(Color.LightGray);
            g.FillEllipse(brush, 0, 0, HeaderHeight * 2, HeaderHeight * 2);
            g.FillEllipse(brush, _width - 2 * HeaderHeight, 0, HeaderHeight * 2, HeaderHeight * 2);
            g.FillRectangle(brush, HeaderHeight, 0, _width - 2 * HeaderHeight, HeaderHeight);

            // Draw main body
            brush = new SolidBrush(Color.GhostWhite);
            g.FillRectangle(brush, 0, HeaderHeight, _width, _height);

            // Draw outline
            var pen = new Pen(Color.DarkGray, 1);
            g.DrawArc(pen, 0, 0, 2 * HeaderHeight, 2 * HeaderHeight, -180, 90);
            g.DrawArc(pen, _width - 2 * HeaderHeight, 0, 2 * HeaderHeight, 2 * HeaderHeight, -0, -91);
            g.DrawLine(pen, HeaderHeight, 0, _width - HeaderHeight, 0);
            g.DrawLine(pen, 0, HeaderHeight, 0, HeaderHeight + _height);
            g.DrawLine(pen, _width, HeaderHeight, _width, HeaderHeight + _height);
            g.DrawLine(pen, 0, HeaderHeight + _height, _width, HeaderHeight + _height);
            g.DrawLine(pen, 0, HeaderHeight, _width, HeaderHeight);

            var headerFormat = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
            var inputFormat = new StringFormat { LineAlignment = StringAlignment.Center };

            // Draw connections
            var connectionBrush = new SolidBrush(Color.Yellow);
            for (var i = 0; i < Inputs.Count; ++i) {
                var topY = HeaderHeight + Padding + i * ConnectionHeight;
                var bottomY = topY + ConnectionHeight;
                var middleY = (topY + bottomY) / 2;
                g.FillEllipse(connectionBrush, -ConnectionRadius, middleY - ConnectionRadius, ConnectionDiameter,
                              ConnectionDiameter);
                g.DrawEllipse(pen, -ConnectionRadius, middleY - ConnectionRadius, ConnectionDiameter, ConnectionDiameter);
                const int r = 2 * ConnectionRadius;
                g.DrawString(Inputs[i].Name, _font, _blackBrush, new RectangleF(r, topY, _width - r, ConnectionHeight),
                             inputFormat);
            }

            if (Output != null) {
                var format = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far };

                var y = HeaderHeight + _height / 2 - ConnectionRadius;
                g.FillEllipse(connectionBrush, _width - ConnectionRadius, y, ConnectionDiameter, ConnectionDiameter);
                g.DrawEllipse(pen, _width - ConnectionRadius, y, ConnectionDiameter, ConnectionDiameter);
                const int r = 2 * ConnectionRadius;
                g.DrawString(Output.Name, _font, _blackBrush,
                             new RectangleF(0, HeaderHeight + _height / 2 - ConnectionHeight / 2, _width - r, ConnectionHeight),
                             format);
            }

            g.DrawString(Name, _font, _blackBrush, new RectangleF(0, 0, _width, HeaderHeight), headerFormat);
        }

        public string Name { get; set; }
        public Point Pos { get; set; }

        public List<Connection> Inputs { get; set; }
        public Connection Output { get; set; }

        private readonly Brush _blackBrush = new SolidBrush(Color.Black);
        private readonly Font _font = new Font("Arial", 7);

        private const int Padding = 10;
        private const int ConnectionHeight = 25;
        private const int ConnectionRadius = 5;
        private const int ConnectionDiameter = 2 * ConnectionRadius;
        private const int HeaderHeight = 20;
        private int _width;
        private int _height;
        private bool _needsUpdate = true;
    }
}
