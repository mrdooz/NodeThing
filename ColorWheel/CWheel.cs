using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ColorWheel
{
    public class CWheel : Control, IDisposable
    {
        #region Constants

        const int _DISPLAY_BUFFER = 2;
        const double _DEGREES_IN_CIRCLE = 360;

        static readonly Color _DEFAULT_COLOR = Color.Red;
        #endregion

        #region Default Fields
        private ColorWheelDisplayStyle _displayStyle = ColorWheelDisplayStyle.ColorWheel;

        private Color _selectedColor = Color.Empty;
        private HorizontalAlignment _alignment = HorizontalAlignment.Left;

        internal float[] Positions { get; set; }
        internal PointF[] Points { get; set; }
        internal Color[] Colors { get; set; }
        internal Region[] Regions { get; set; }
        internal PointF[] InnerPoints { get; set; }
        internal GraphicsPath WheelPath { get; set; }

        private double _segmentRotation = 3;

        SmoothingMode _mode = SmoothingMode.HighQuality;

        bool _drawselector = true;

        #endregion

        /// <summary>
        /// Constructor for the CWheel class.
        /// </summary>
        public CWheel()
        {
            //SetStyle(ControlStyles.AllPaintingInWmPaint, ControlStyles.OptimizedDoubleBuffer, true);
            this.TabStop = false;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            //ColorWheelNET.Renderers.SubscriptionManager.Subscribe(this);
        }


        #region Custom Events

        /// <summary>
        /// Delegate for the SelectedColorChanged event
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="eventArgs">Event Args to pass to the event handler</param>
        public delegate void SelectedColorChangedHandler(object sender, SelectedColorChangedEventArgs eventArgs);
        /// <summary>
        /// Event that fires after the selected color has changed.
        /// </summary>
        public event SelectedColorChangedHandler SelectedColorChanged;

        /// <summary>
        /// Delegate for the Cancelable SelectedColorChanging event
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="eventArgs">Event Args to pass to the event handler</param>
        public delegate void SelectedColorChangingHandler(object sender, SelectedColorChangingEventArgs eventArgs);
        /// <summary>
        /// Cancelable Event That will Fire when the Color is in the process of Changing.
        /// </summary>
        public event SelectedColorChangingHandler SelectedColorChanging;

        #endregion


        #region subscription Model

        public delegate void DrawColorWheelCall(CWheel wheel, Graphics g);
        public delegate void DrawSelectorCall(CWheel wheel, Graphics g);

        public delegate void CalculateSelectedPointCall(CWheel wheel);

        public delegate void ColorHitTestCall(CWheel wheel, Point pt);

        public delegate void CalculateColorGroupCall(CWheel wheel, Point pt);

        public delegate void CalculateWheelLayoutCall(CWheel wheel);

        public delegate void CalculateSelectorColorCall(CWheel wheel);

        public DrawColorWheelCall OnDrawColorWheel;
        public DrawSelectorCall OnDrawSelector;
        public CalculateSelectedPointCall OnCalculateSelectedPoint;
        public ColorHitTestCall OnColorHitTest;
        public CalculateColorGroupCall OnCalculateColorGroup;
        public CalculateWheelLayoutCall OnCalculateLayout;
        public CalculateSelectorColorCall OnCalculateSelectorColor;


        internal void Unsubscribe()
        {
            OnDrawColorWheel = null;
            OnDrawSelector = null;
            OnCalculateSelectedPoint = null;
            OnColorHitTest = null;
            OnCalculateColorGroup = null;
            OnCalculateLayout = null;
            OnCalculateSelectorColor = null;
        }

        internal void ResetInvalidate()
        {
            this.WheelPath = null;
            this.SelectedPoint = Point.Empty;
            this.Colors = null;
            this.Points = null;
            this.SelectorColor = Color.Empty;
            this.Regions = null;
            this.WheelInnerRectangle = Rectangle.Empty;
            this.InnerPoints = null;
            this.ColorGroup = Color.Empty;

            this.CalculateLayout();
            this.Invalidate();
        }

        #endregion

        /// <summary>
        /// Gets or Sets the Style of the Color Wheel
        /// </summary>
        public ColorWheelDisplayStyle DisplayStyle
        {
            get { return _displayStyle; }
            set
            {
                if (_displayStyle != value)
                {
                    _displayStyle = value;
                    //ColorWheelNET.Renderers.SubscriptionManager.Subscribe(this);
                    this.ResetInvalidate();
                }
            }
        }

        /// <summary>
        /// Gets or Sets whether the Selector ring should be drawn on the color wheel around the selected color
        /// </summary>
        public bool ShowSelector
        {
            get { return _drawselector; }
            set
            {
                if (_drawselector != value)
                {
                    _drawselector = value;
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// Rotates the wheel based on X amount of segmants
        /// 
        /// Defaults to 3, so upper right corner is blue instead of yellow
        /// </summary>
        public double SegmentRotation
        {
            get { return _segmentRotation; }
            set
            {
                if (_segmentRotation != value)
                {
                    _segmentRotation = value;
                    CalculateLayout();
                    this.Invalidate();
                }
            }
        }


        /// <summary>
        /// Horizontal Alignment of wheel
        /// </summary>
        public HorizontalAlignment Alignment
        {
            get { return _alignment; }
            set
            {
                if (_alignment != value)
                {
                    _alignment = value;
                    CalculateLayout();
                    this.Invalidate();
                }
            }
        }

        /// <summary>
        /// Gets or Sets the smoothing Mode of the Color Wheel
        /// </summary>
        public SmoothingMode SmoothingMode
        {
            get { return _mode; }
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    this.Invalidate();
                }
            }
        }

        internal PointF WheelCenter { get; set; }

        /// <summary>
        /// The Rectangle to draw the Wheel
        /// </summary>
        internal Rectangle WheelRectangle
        {
            get
            {

                Rectangle rec = new Rectangle
                    (_DISPLAY_BUFFER, _DISPLAY_BUFFER,
                    (this.DisplayRectangle.Width - _DISPLAY_BUFFER * 2) > 0 ? (this.DisplayRectangle.Width - _DISPLAY_BUFFER * 2) : 2,
                    (this.DisplayRectangle.Height - _DISPLAY_BUFFER * 2) > 0 ? (this.DisplayRectangle.Height - _DISPLAY_BUFFER * 2) : 2
                    );

                if (rec.Height > rec.Width)
                {
                    rec.Height = rec.Width;
                }
                else
                {
                    rec.Width = rec.Height;
                }

                if (this.DisplayRectangle.Height != this.DisplayRectangle.Width)
                {
                    int nX = 0;
                    switch (Alignment)
                    {
                        case HorizontalAlignment.Left:
                            break;
                        case HorizontalAlignment.Center:
                            nX = (this.DisplayRectangle.Width / 2) - (rec.Width / 2);
                            rec.X = nX > 0 ? nX : rec.X;
                            break;
                        case HorizontalAlignment.Right:
                            nX = this.DisplayRectangle.Width - rec.Width - _DISPLAY_BUFFER;
                            rec.X = nX > 0 ? nX : rec.X;
                            break;
                    }
                }

                float centerX = rec.Right - (rec.Width / 2);
                float centerY = rec.Bottom - (rec.Height / 2);

                //set the center point
                this.WheelCenter = new PointF(
                            centerX > 0 ? centerX : rec.X,
                            centerY > 0 ? centerY : rec.Y
                            );

                return rec;
            }
        }


        internal Rectangle WheelInnerRectangle { get; set; }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (this.Dock == DockStyle.None)
            {
                this.Height = this.Width;
            }

            this.Invalidate();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.SelectedPoint = Point.Empty;
            CalculateLayout();
        }

        /// <summary>
        /// Gets or Sets the selected Color of the ColorWheel.
        /// Color.Empty is not accepted
        /// </summary>
        public Color Color
        {
            get { return _selectedColor; }
            set
            {
                SetSelectedColor(value, Point.Empty, Color.Empty);
                ResetInvalidate();
            }
        }

        Color _colorGroup;

        /// <summary>
        /// Gets the Hue group the Selected Color belongs to
        /// </summary>
        public Color ColorGroup
        {
            get
            {
                if (this.Color.IsEmpty || this.Colors == null) { return Color.Empty; }
                if (_colorGroup.IsEmpty)
                {
                    if(OnCalculateColorGroup != null)
                        OnCalculateColorGroup(this, this.SelectedPoint);
                }

                return _colorGroup;
            }
            internal set
            {
                _colorGroup = value;
            }
        }


        internal bool SetSelectedColor(Color value, Point point, Color group)
        {
            if (value.IsEmpty)
            {
                value = _DEFAULT_COLOR;
            }

            if (OnCancelColorChanging(value)) { return false; }

            _selectedColor = value;
            SelectedPoint = point;

            this.ColorGroup = group;

            OnColorChanged();


            if (OnCalculateSelectorColor != null)
                OnCalculateSelectorColor(this);

            this.Invalidate();
            return true;
        }

       
        internal Color SelectorColor { get; set; }


        Point _selPoint = Point.Empty;
        internal Point SelectedPoint
        {
            get
            {
                if (_selPoint.IsEmpty && OnCalculateSelectedPoint != null)
                    OnCalculateSelectedPoint(this);
                return _selPoint;
            }
            set
            {
                _selPoint = value;
            }
        }

        /// <summary>
        /// Fire the Color Changed Event if it is wired
        /// </summary>
        protected virtual void OnColorChanged()
        {
            if (SelectedColorChanged != null)
            {
                SelectedColorChangedEventArgs cea = new SelectedColorChangedEventArgs(this.Color);
                SelectedColorChanged(this, cea);
            }
        }

        /// <summary>
        /// Returns true if the color changing was canceled
        /// </summary>
        /// <param name="value">The Color we are changing to</param>
        /// <returns>True if the Color Changing should cancel</returns>
        protected virtual bool OnCancelColorChanging(Color value)
        {
            if (SelectedColorChanging != null)
            {
                SelectedColorChangingEventArgs cea = new SelectedColorChangingEventArgs(value);
                SelectedColorChanging(this, cea);
                if (cea.Cancel) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Handles all CWheel Drawing. Including Selector and Wheel.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            lock (e.Graphics)
            {
                try
                {
                    e.Graphics.Clear(this.BackColor);
                    //for performance reasons
                    if (this.DesignMode)
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    }
                    else
                    {
                        e.Graphics.SmoothingMode = this.SmoothingMode;
                    }

                    if (OnDrawColorWheel != null)
                        OnDrawColorWheel(this, e.Graphics);
                    if (OnDrawSelector != null)
                        OnDrawSelector(this, e.Graphics);
                }
                catch
                {
                    e.Graphics.Clear(this.BackColor);
                }
            }
        }


        private void CalculateLayout()
        {
            if (OnCalculateLayout != null)
                OnCalculateLayout(this);
        }


        protected override void Dispose(bool disposing)
        {
            if(!this.IsDisposed)
                Unsubscribe();
            base.Dispose(disposing);
        }


         #region event overrides

         protected override void OnMouseClick(MouseEventArgs e)
         {
             base.OnMouseClick(e);

             if (e.Button == System.Windows.Forms.MouseButtons.Left)
             {
                 _mouseDown = false;
                 if(OnColorHitTest != null)
                    OnColorHitTest(this, e.Location);
             }
         }

         bool _mouseDown = false;

         internal bool WheelMouseDown { get; set; }
         internal bool WheelMouseDown2 { get; set; }

         protected override void OnMouseDown(MouseEventArgs e)
         {
             base.OnMouseDown(e);

             if (e.Button == System.Windows.Forms.MouseButtons.Left)
             {
                 _mouseDown = true;
                 if (OnColorHitTest != null)
                     OnColorHitTest(this, e.Location);
             }
         }


         protected override void OnMouseUp(MouseEventArgs e)
         {
             base.OnMouseUp(e);
             _mouseDown = false;
             WheelMouseDown2 = WheelMouseDown = false;
         }

         protected override void OnMouseMove(MouseEventArgs e)
         {
             base.OnMouseMove(e);

             if (_mouseDown && e.Button == System.Windows.Forms.MouseButtons.Left)
             {
                 if (OnColorHitTest != null)
                     OnColorHitTest(this, e.Location);
             }
         }

         protected override void OnMouseLeave(EventArgs e)
         {
             base.OnMouseLeave(e);
             _mouseDown = false;
             WheelMouseDown2 = WheelMouseDown = false;
         }


         protected override void OnLostFocus(EventArgs e)
         {
             base.OnLostFocus(e);
             _mouseDown = false;
             WheelMouseDown2 = WheelMouseDown = false;
         }


        #endregion
    }


    #region Extra classes

    /// <summary>
    /// Cancelable Event that fires when a Color is changing.
    /// 
    /// Setting Cancel = True; cancels the color from changing.
    /// </summary>
    public class SelectedColorChangingEventArgs : CancelEventArgs
    {
        /// <summary>
        /// The Color changing to
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Constructor for Cancelable color changing event.
        /// </summary>
        /// <param name="color">The Color changing to</param>
        public SelectedColorChangingEventArgs(Color color) : base()
        {
            this.Color = color;
        }
    }

    /// <summary>
    /// Event that fires after a Color has changed.
    /// </summary>
    public class SelectedColorChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The Color changed to.
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Constructor for the changed event.
        /// </summary>
        /// <param name="color">The Color changed to.</param>
        public SelectedColorChangedEventArgs(Color color)
            : base()
        {
            this.Color = color;
        }
    }

    /// <summary>
    /// Display style for a Color Wheel
    /// </summary>
    public enum ColorWheelDisplayStyle
    {
        /// <summary>
        /// The standard traditional round Color Wheel.
        /// White in Center, full color on outer portions of wheel.
        /// </summary>
        ColorWheel = 0,
        /// <summary>
        /// Non standard round wheel with a dark center.
        /// Black in Center, full color on outer portions of wheel.
        /// </summary>
        DarkColorWheel = 1,
        /// <summary>
        /// Hue Ring with a Inner Triangle for Saturation and Value selection
        /// </summary>
        HSVTriangle = 2,
        /// <summary>
        /// Traditional Rectangle Gradient for Color selection
        /// </summary>
        Rectangle = 3
    }

    #endregion

}
