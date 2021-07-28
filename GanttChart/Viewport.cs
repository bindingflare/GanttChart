using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Edcore.GanttChart
{
    /// <summary>
    /// IViewport moves in world coordinate and projects models to device coordinate space
    /// </summary>
    public interface IViewport
    {
        /// <summary>
        /// Get the projection matrix to transform world coordinates to device coordinates
        /// </summary>
        Matrix Projection { get; }
        /// <summary>
        /// Get the rectangle boundary in world coordinates to be captured and projected onto viewport
        /// </summary>
        RectangleF Rectangle { get; }
        /// <summary>
        /// Request viewport to recalculate its get properties.
        /// </summary>
        void Resize();
        /// <summary>
        /// Convert device coordinates to world coordinates
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        PointF DeviceToWorldCoord(Point screencoord);
        /// <summary>
        /// Convert device coordinates to world coordinates
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        PointF DeviceToWorldCoord(PointF screencoord);
        /// <summary>
        /// Convert world coordinates to device coordinates
        /// </summary>
        /// <param name="worldcoord"></param>
        /// <returns></returns>
        PointF WorldToDeviceCoord(PointF worldcoord);
        /// <summary>
        /// Get or set the world height
        /// </summary>
        float WorldHeight { get; set; }
        /// <summary>
        /// Get or set the world width
        /// </summary>
        float WorldWidth { get; set; }
        /// <summary>
        /// Get or set the X world-coordinate of the position of the viewport.
        /// This is also the same as the IViewport.Rectangle.Left.
        /// </summary>
        float X { get; set; }
        /// <summary>
        /// Get or set the Y world-coordinate of the position of the viewport.
        /// This is also the same as the IViewport.Rectangle.Top.
        /// </summary>
        float Y { get; set; }
    }

    /// <summary>
    /// IViewport for printing to file
    /// </summary>
    class PrintViewport : IViewport
    {
        public PrintViewport(Graphics graphics,
            float worldWidth, float worldHeight,
            int deviceWidth, int deviceHeight,
            int marginLeft, int marginTop)
        {
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;

            _mDeviceWidth = deviceWidth;
            _mDeviceHeight = deviceHeight;

            _mMarginTop = marginTop;
            _mMarginLeft = marginLeft;
        }

        /// <summary>
        /// Get or set viewport X world coordinate
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Get or set viewport Y world coordinate
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Get or set scaling factor between 0.0f and 1.0f (obviously, scale of zero would mean you can't see anything)
        /// </summary>
        public float Scale { get { return _mScale; } set { _mScale = value; } }

        /// <summary>
        /// Get or set width of the world
        /// </summary>
        public float WorldWidth { get; set; }

        /// <summary>
        /// Get or set height of the world
        /// </summary>
        public float WorldHeight { get; set; }

        /// <summary>
        /// Get the projection matrix for transforming models into viewport
        /// </summary>
        public Matrix Projection
        {
            get
            {
                Matrix matrix = new Matrix();
                matrix.Translate(this._mMarginLeft, this._mMarginTop);
                matrix.Scale(_mScale, _mScale);
                matrix.Translate(-this.X, -this.Y);
                return matrix;
            }
        }

        /// <summary>
        /// Get the rectangle bounds of the viewport in world coordinate
        /// </summary>
        public RectangleF Rectangle
        {
            get { return new RectangleF(this.X, this.Y, _mDeviceWidth / _mScale, _mDeviceHeight / _mScale); }
        }

        /// <summary>
        /// Resize the viewport, recalculating and correcting dimensions
        /// </summary>
        public void Resize()
        {
            
            
        }

        /// <summary>
        /// Convert view coordinates to world coordinate
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        public PointF DeviceToWorldCoord(Point screencoord)
        {
            return new PointF(screencoord.X + X, screencoord.Y + Y);
        }

        /// <summary>
        /// Convert view coordinates to world coordinate
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        public PointF DeviceToWorldCoord(PointF screencoord)
        {
            return new PointF(screencoord.X + X, screencoord.Y + Y);
        }

        /// <summary>
        /// Convert world coordinates to view coordinate
        /// </summary>
        /// <param name="worldcoord"></param>
        /// <returns></returns>
        public PointF WorldToDeviceCoord(PointF worldcoord)
        {
            return new PointF(worldcoord.X - X, worldcoord.Y - Y);
        }

        int _mDeviceWidth, _mDeviceHeight;
        int _mMarginLeft, _mMarginTop;
        float _mScale = 1.0f;
    }

    /// <summary>
    /// An IViewport that is placed over a world coordinate system and provides methods to transform between world and view coordinates
    /// </summary>
    public class ControlViewport : IViewport
    {
        /// <summary>
        /// Construct a Viewport
        /// </summary>
        /// <param name="view"></param>
        public ControlViewport(Control view)
        {
            _mDevice = view;
            HorizontalScroll = new HScrollBar();
            VerticalScroll = new VScrollBar();
            _mScrollHolePatch = new UserControl();
            WorldWidth = view.Width;
            WorldHeight = view.Height;

            _mDevice.Controls.Add(HorizontalScroll);
            _mDevice.Controls.Add(VerticalScroll);
            _mDevice.Controls.Add(_mScrollHolePatch);

            HorizontalScroll.Scroll += (s, e) => X = e.NewValue;
            VerticalScroll.Scroll += (s, e) => Y = e.NewValue;
            _mDevice.Resize += (s, e) => this.Resize();
            _mDevice.MouseWheel += (s, e) => Y -= e.Delta > 0 ? WheelDelta : -WheelDelta;
            WheelDelta = VerticalScroll.LargeChange;

            _RecalculateMatrix();
            _RecalculateRectangle();
        }

        /// <summary>
        /// Identity Matrix
        /// </summary>
        public static readonly Matrix Identity = new Matrix(1, 0, 0, 1, 0, 0); 

        /// <summary>
        /// Get or set the number of pixels to scroll on each click of the mouse
        /// </summary>
        public int WheelDelta { get; set; }

        public HScrollBar HorizontalScroll { get; set; }
        public VScrollBar VerticalScroll { get; set; }

        /// <summary>
        /// Get the Rectangle area in world coordinates where the Viewport is currently viewing over
        /// </summary>
        public RectangleF Rectangle
        {
            get
            {
                return _mRectangle;
            }
        }

        /// <summary>
        /// Get the projection transformation matrix required for drawing models in the world projected into view
        /// </summary>
        public Matrix Projection
        {
            get
            {
                return _mMatrix;
            }
        }

        /// <summary>
        /// Resize the Viewport according to the view control and world dimensions, which ever larger and add scrollbars where approperiate
        /// </summary>
        public void Resize()
        {
            HorizontalScroll.Dock = DockStyle.None;
            HorizontalScroll.Location = new Point(0, _mDevice.Height - HorizontalScroll.Height);
            HorizontalScroll.Width = _mDevice.Width - VerticalScroll.Width;
            
            VerticalScroll.Dock = DockStyle.None;
            VerticalScroll.Location = new Point(_mDevice.Width - VerticalScroll.Width, 0);
            VerticalScroll.Height = _mDevice.Height - HorizontalScroll.Height;

            _mScrollHolePatch.Location = new Point(HorizontalScroll.Right, VerticalScroll.Bottom);
            _mScrollHolePatch.Size = new Size(VerticalScroll.Width, HorizontalScroll.Height);

            if (WorldWidth <= _mDevice.Width)
            {
                HorizontalScroll.Hide();
            }
            else
            {
                HorizontalScroll.Maximum = (int)(WorldWidth - _mDevice.Width);
                HorizontalScroll.Show();

            }

            if (WorldHeight <= _mDevice.Height)
            {
                VerticalScroll.Hide();
            }
            else
            {
                VerticalScroll.Maximum = (int)(WorldHeight - _mDevice.Height);
                //VerticalScroll.Show();
            }

            _RecalculateRectangle();
            _RecalculateMatrix();

            _mDevice.Invalidate();
        }

        /// <summary>
        /// Convert view coordinates to world coordinates
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        public PointF DeviceToWorldCoord(Point screencoord)
        {
            return new PointF(screencoord.X + X, screencoord.Y + Y);
        }

        /// <summary>
        /// Convert view coordinates to world coordinates
        /// </summary>
        /// <param name="screencoord"></param>
        /// <returns></returns>
        public PointF DeviceToWorldCoord(PointF screencoord)
        {
            return new PointF(screencoord.X + X, screencoord.Y + Y);
        }

        /// <summary>
        /// Convert world coordinates to view coordinates
        /// </summary>
        /// <param name="worldcoord"></param>
        /// <returns></returns>
        public PointF WorldToDeviceCoord(PointF worldcoord)
        {
            return new PointF(worldcoord.X - X, worldcoord.Y - Y);
        }

        /// <summary>
        /// Get or set the world width
        /// </summary>
        public float WorldWidth
        {
            get { return _mWorldWidth; }
            set
            {
                if (!value.Equals(_mWorldWidth))
                {
                    if (value < _mDevice.Width) value = _mDevice.Width;
                    _mWorldWidth = value;
                    Resize();
                }
            }
        }

        /// <summary>
        /// Get or set the world height
        /// </summary>
        public float WorldHeight
        {
            get { return _mWorldHeight; }
            set
            {
                if (!value.Equals(_mWorldHeight))
                {
                    if (value < _mDevice.Height) value = _mDevice.Height;
                    _mWorldHeight = value;
                    Resize();
                }
            }
        }

        /// <summary>
        /// Get or set the world X coordinate of the Viewport location, represented by the top left corner of the Viewport Rectangle
        /// </summary>
        public float X
        {
            get { return HorizontalScroll.Value; }
            set
            {
                if (!((int)value).Equals(HorizontalScroll.Value))
                {
                    if (value > HorizontalScroll.Maximum) value = HorizontalScroll.Maximum;
                    else if (value < 0) value = 0;
                    HorizontalScroll.Value = (int)value;
                    _RecalculateRectangle();
                    _RecalculateMatrix();
                    _mDevice.Invalidate();
                }
            }
        }

        /// <summary>
        /// Get or set the wordl Y coordinate of the Viewport location, represented by the top left corner of the Viewport Rectangle
        /// </summary>
        public float Y
        {
            get { return VerticalScroll.Value; }
            set
            {
                if (!((int)value).Equals(VerticalScroll.Value))
                {
                    if (value > VerticalScroll.Maximum) value = VerticalScroll.Maximum;
                    else if (value < 0) value = 0;
                    VerticalScroll.Value = (int)value;
                    _RecalculateRectangle();
                    _RecalculateMatrix();
                    _mDevice.Invalidate();
                }
            }
        }

        private void _RecalculateRectangle()
        {
            _mRectangle = new RectangleF(X, Y, _mDevice.Width, _mDevice.Height);
        }

        private void _RecalculateMatrix()
        {
            _mMatrix = new Matrix();
            _mMatrix.Translate(-X, -Y);
        }

        Control _mDevice;
        UserControl _mScrollHolePatch;
        RectangleF _mRectangle = RectangleF.Empty;
        Matrix _mMatrix = new Matrix();
        float _mWorldHeight, _mWorldWidth;
    }

    /// <summary>
    /// IViewport for printing to image. The full chart in the world is printed as-is.
    /// </summary>
    class ImageViewport : IViewport
    {
        private float _mScale;

        public ImageViewport(float scale, float worldWidth, float worldHeight)
        {
            _mScale = scale;
            WorldWidth = worldWidth;
            WorldHeight = worldHeight;
        }

        /// <summary>
        /// Get the projection matrix to transform world coordinates to device coordinates
        /// </summary>
        public Matrix Projection
        {
            get
            {
                Matrix projection = new Matrix();
                projection.Scale(_mScale, _mScale);
                return projection;
            }
        }

        /// <summary>
        /// The device rectangle boundary in the world to capture and project onto device is the same rectangle of the world for ImageViewport
        /// So that the entire world is projected onto the ImageViewport
        /// </summary>
        public RectangleF Rectangle
        {
            get
            {
                return new RectangleF(0, 0, this.WorldWidth, this.WorldHeight);
            }
        }

        public void Resize()
        {
            
        }

        /// <summary>
        /// The device and world coordinates are the same for ImageViewport
        /// </summary>
        public PointF DeviceToWorldCoord(Point screencoord)
        {
            return screencoord;
        }

        /// <summary>
        /// The device and world coordinates are the same for ImageViewport
        /// </summary>
        public PointF DeviceToWorldCoord(PointF screencoord)
        {
            return screencoord;
        }

        /// <summary>
        /// The device and world coordinates are the same for ImageViewport
        /// </summary>
        public PointF WorldToDeviceCoord(PointF worldcoord)
        {
            return worldcoord;
        }

        /// <summary>
        /// Get or set the world height
        /// </summary>
        public float WorldHeight { get; set; }

        /// <summary>
        /// Get or set the world width
        /// </summary>
        public float WorldWidth { get; set; }

        /// <summary>
        /// No effect for ImageViewport. X and Y world-coordinate offset is always at (0,0).
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// No effect for ImageViewport. X and Y world-coordinate offset is always at (0,0).
        /// </summary>
        public float Y { get; set; }
    }
}
