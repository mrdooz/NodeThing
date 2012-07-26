using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace NodeThing
{
    public class Node
    {
        public Node()
        {
            inputs = new List<Connection>();
        }

        int calcWidth(Graphics g)
        {
            var defaultWidth = 100;
            var maxInput = 0;
            foreach (var input in inputs)
            {
                var bounds = g.MeasureString(input.name, _font);
                maxInput = Math.Max(maxInput, (int)(bounds.Width + 0.5));
            }

            if (output != null)
            {
                var bounds = g.MeasureString(output.name, _font);
                maxInput += _padding + (int)bounds.Width + 4 * _connectionRadius;
            }
            else
            {
                maxInput += 2 * _connectionRadius;
            }

            return Math.Max(defaultWidth, maxInput);
        }

        public Connection pointInsideConnection(Point pt)
        {
            var x = pos.X;
            var y = pos.Y;

            for (var i = 0; i < inputs.Count; ++i)
            {
                var topY = _headerHeight + _padding + i * _connectionHeight;
                var bottomY = topY + _connectionHeight;
                var middleY = (topY + bottomY) / 2;

                var dx = pt.X - x;
                var dy = pt.Y - (y + middleY);
                if (Math.Sqrt(dx * dx + dy * dy) < _connectionRadius)
                    return inputs[i];
            }

            if (output != null)
            {
                var dx = pt.X - (x + _width);
                var dy = pt.Y - (y + _headerHeight + _height / 2);
                if (Math.Sqrt(dx * dx + dy * dy) < _connectionRadius)
                    return output;
            }

            return null;
        }

        public bool pointInsideBody(Point pt)
        {
            var x = pos.X;
            var y = pos.Y;

            var dx = pt.X - (x + _connectionRadius);
            var dy = pt.Y - (y + _connectionRadius);
            var leftDist = Math.Sqrt(dx * dx + dy * dy);

            dx = pt.X - (x + _width - _connectionRadius);
            var rightDist = Math.Sqrt(dx*dx + dy*dy);

            var middle = new Rectangle(x + _connectionRadius, y, _width - 2 * _connectionRadius, _headerHeight);
            var body = new Rectangle(pos.X, pos.Y + _headerHeight, _width, _height);

            return body.Contains(pt) || middle.Contains(pt) || leftDist < _connectionRadius || rightDist < _connectionRadius;
        }

        public void render(Graphics g)
        {
            g.ResetTransform();
            g.TranslateTransform(pos.X, pos.Y);

            if (_needsUpdate)
            {
                _needsUpdate = false;
                var numSlots = Math.Max(inputs.Count, output != null ? 1 : 0);
                _height = 2 * _padding + _connectionHeight * numSlots;
                _width = calcWidth(g);
            }

            // Draw header
            var brush = new SolidBrush(Color.LightGray);
            g.FillEllipse(brush, 0, 0, _headerHeight * 2, _headerHeight * 2);
            g.FillEllipse(brush, _width - 2 * _headerHeight, 0, _headerHeight * 2, _headerHeight * 2);
            g.FillRectangle(brush, _headerHeight, 0, _width - 2 * _headerHeight, _headerHeight);

            // Draw main body
            brush = new SolidBrush(Color.GhostWhite);
            g.FillRectangle(brush, 0, _headerHeight, _width, _height);

            // Draw outline
            var pen = new Pen(Color.DarkGray, 1);
            g.DrawArc(pen, 0, 0, 2 * _headerHeight, 2 * _headerHeight, -180, 90);
            g.DrawArc(pen, _width - 2 * _headerHeight, 0, 2 * _headerHeight, 2 * _headerHeight, -0, -91);
            g.DrawLine(pen, _headerHeight, 0, _width - _headerHeight, 0);
            g.DrawLine(pen, 0, _headerHeight, 0, _headerHeight + _height);
            g.DrawLine(pen, _width, _headerHeight, _width, _headerHeight + _height);
            g.DrawLine(pen, 0, _headerHeight + _height, _width, _headerHeight + _height);
            g.DrawLine(pen, 0, _headerHeight, _width, _headerHeight);

            var headerFormat = new StringFormat();
            headerFormat.LineAlignment = StringAlignment.Center;
            headerFormat.Alignment = StringAlignment.Center;

            var inputFormat = new StringFormat();
            inputFormat.LineAlignment = StringAlignment.Center;

            // Draw connections
            var connectionBrush = new SolidBrush(Color.Yellow);
            for (var i = 0; i < inputs.Count; ++i)
            {
                var topY = _headerHeight + _padding + i * _connectionHeight;
                var bottomY = topY + _connectionHeight;
                var middleY = (topY + bottomY) / 2;
                g.FillEllipse(connectionBrush, -_connectionRadius, middleY - _connectionRadius, _connectionDiameter, _connectionDiameter);
                g.DrawEllipse(pen, -_connectionRadius, middleY - _connectionRadius, _connectionDiameter, _connectionDiameter);
                var r = 2 * _connectionRadius;
                g.DrawString(inputs[i].name, _font, _blackBrush, new RectangleF(r, topY, _width-r, _connectionHeight), inputFormat);
            }

            if (output != null)
            {
                var format = new StringFormat();
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Far;

                var y = _headerHeight + _height / 2 - _connectionRadius;
                g.FillEllipse(connectionBrush, _width - _connectionRadius, y, _connectionDiameter, _connectionDiameter);
                g.DrawEllipse(pen, _width - _connectionRadius, y, _connectionDiameter, _connectionDiameter);
                var r = 2 * _connectionRadius;
                g.DrawString(output.name, _font, _blackBrush, 
                    new RectangleF(0, _headerHeight + _height / 2 - _connectionHeight / 2, _width-r, _connectionHeight), format);
            }

            g.DrawString(name, _font, _blackBrush, new RectangleF(0, 0, _width, _headerHeight), headerFormat);
        }

        public string name { get; set; }
        public Point pos { get; set; }

        public List<Connection> inputs { get; set; }
        public Connection output { get; set; }

        Brush _blackBrush = new SolidBrush(Color.Black);
        Font _font = new Font("Arial", 7);

        const int _padding = 10;
        const int _connectionHeight = 25;
        const int _connectionRadius = 5;
        const int _connectionDiameter = 2 * _connectionRadius;
        const int _headerHeight = 20;
        int _width;
        int _height;
        bool _needsUpdate = true;

    }
}
