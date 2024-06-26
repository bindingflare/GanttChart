﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;

namespace Edcore.GanttChart
{
    static class GDIExtention
    {
        public static void DrawRectangle(this Graphics graphics, Pen pen, RectangleF rectangle)
        {
            graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        public static RectangleF TextBoxAlign(this Graphics graphics, string text, ChartTextAlign align, Font font, RectangleF textbox, float margin = 0)
        {
            var size = graphics.MeasureString(text, font);
            if (align == ChartTextAlign.MiddleCenter)
            {
                return new RectangleF(new PointF(textbox.Left + (textbox.Width - size.Width) / 2, textbox.Top + (textbox.Height - size.Height) / 2), size);
            }
            else if (align == ChartTextAlign.MiddleLeft)
            {
                return new RectangleF(new PointF(textbox.Left + margin, textbox.Top + (textbox.Height - size.Height) / 2), size);
            }
            else
            {
                throw new NotImplementedException("Need to implement more alignment types");
            }
        }
    }

    /// <summary>
    /// Gantt Chart control
    /// </summary>
    public partial class GanttChart : UserControl
    {

        #region Public Methods

        /// <summary>
        /// Construct a gantt chart
        /// </summary>
        public GanttChart()
        {
            // Designer values
            InitializeComponent();

            // Factory values
            HeaderOneHeight = 33;
            HeaderTwoHeight = 26;
            BarSpacing = 32;
            BarHeight = 23;
            MajorWidth = 140;
            MinorWidth = 20;
            TimeResolution = TimeResolution.Day;
            this.DoubleBuffered = true;
            Viewport = new ControlViewport(this) { WheelDelta = BarSpacing };
            //Viewport.RegisterScrollEvent(GanttChart_OnScroll);
            AllowTaskDragDrop = true;
            ShowRelations = true;
            ShowSlack = false;
            AccumulateRelationsOnGroup = false;
            showCompletionLabels = true;
            //this.Dock = DockStyle.Fill;
            this.Margin = new Padding(0, 0, 0, 0);
            this.Padding = new Padding(0, 0, 0, 0);
            // Formatting
            HatchBrush myHatchBrush = new HatchBrush(HatchStyle.ForwardDiagonal, Color.PaleVioletRed, Color.Transparent);

            TaskFormat = new Edcore.GanttChart.TaskFormat()
            {
                Color = Brushes.Black,
                Border = Pens.Maroon,
                BackFill = Brushes.MediumSlateBlue,
                ForeFill = Brushes.YellowGreen,
                DelayBackFill = myHatchBrush,
                DelayForeFill = Brushes.PaleVioletRed,
                SlackFill = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.LightDownwardDiagonal, Color.Blue, Color.Transparent)
            };
            CriticalTaskFormat = new Edcore.GanttChart.TaskFormat()
            {
                Color = Brushes.Black,
                Border = Pens.Maroon,
                BackFill = Brushes.Crimson,
                ForeFill = Brushes.YellowGreen,
                DelayBackFill = myHatchBrush,
                DelayForeFill = Brushes.PaleVioletRed,
                SlackFill = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.LightDownwardDiagonal, Color.Red, Color.Transparent)
            };
            HeaderFormat = new Edcore.GanttChart.HeaderFormat()
            {
                Color = Brushes.Black,
                Border = new Pen(SystemColors.ActiveBorder),
                GradientLight = SystemColors.ButtonHighlight,
                GradientDark = SystemColors.ButtonFace
            };
        }

        /// <summary>
        /// Delegate method for creating a new Task. Creates Task by default.
        /// </summary>
        public Func<Task> CreateTaskDelegate = delegate () { return new Task(); };

        /// <summary>
        /// Get the selected tasks.
        /// Split tasks will not be in this list, only its task parts, if selected.
        /// </summary>
        public IEnumerable<Task> SelectedTasks
        {
            get
            {
                return _mSelectedTasks.ToArray();
            }
        }

        /// <summary>
        /// Get the latest selected task
        /// </summary>
        public Task SelectedTask
        {
            get
            {
                return _mSelectedTasks.LastOrDefault();
            }
        }

        public Dictionary<Task, RectangleF> GetTaskRectangles()
        {
            return _mChartTaskRects;
        }

        /// <summary>
        /// Get or set header1 pixel height
        /// </summary>
        [DefaultValue(32)]
        public int HeaderOneHeight { get; set; }

        /// <summary>
        /// Get or set header2 pixel height
        /// </summary>
        [DefaultValue(23)]
        public int HeaderTwoHeight { get; set; }

        /// <summary>
        /// Get or set pixel distance from top of each Task to the next
        /// </summary>
        [DefaultValue(32)]
        public int BarSpacing { get; set; }

        /// <summary>
        /// Get or set pixel height of each Task
        /// </summary>
        [DefaultValue(28)]
        public int BarHeight { get; set; }

        /// <summary>
        /// Get or set the time scale display format
        /// </summary>
        [DefaultValue(TimeResolution.Day)]
        public TimeResolution TimeResolution { get; set; }

        /// <summary>
        /// Get or set the pixel width of each step of the time scale e.g. if TimeScale is TimeScale.Day, then each Day will be TimeWidth pixels apart
        /// </summary>
        [DefaultValue(20)]
        public int MinorWidth { get; set; }

        /// <summary>
        /// Get or set pixel width between major tick marks.
        /// </summary>
        [DefaultValue(140)]
        public int MajorWidth { get; set; }

        /// <summary>
        /// Get or set format for Tasks
        /// </summary>
        public TaskFormat TaskFormat { get; set; }

        /// <summary>
        /// Get or set format for critical Tasks
        /// </summary>
        public TaskFormat CriticalTaskFormat { get; set; }

        /// <summary>
        /// Get or set format for headers
        /// </summary>
        public HeaderFormat HeaderFormat { get; set; }

        /// <summary>
        /// Get or set format for relations
        /// </summary>
        public RelationFormat RelationFormat { get; set; }

        /// <summary>
        /// Get or set whether dragging of Tasks is allowed. Set to false when not dragging to skip drag(drop) tracking.
        /// </summary>
        [DefaultValue(true)]
        public bool AllowTaskDragDrop { get; set; }

        /// <summary>
        /// Get or set whether to show relations
        /// </summary>
        [DefaultValue(true)]
        public bool ShowRelations { get; set; }

        /// <summary>
        /// Get or set whether to show task labels
        /// </summary>
        [DefaultValue(true)]
        public bool showCompletionLabels { get; set; }

        /// <summary>
        /// Get or set whether to accumulate relations on group tasks and show relations even when group is collapsed. (Not working well; still improving on it)
        /// </summary>
        [DefaultValue(false)]
        public bool AccumulateRelationsOnGroup { get; set; }

        /// <summary>
        /// Get or set whether to show slack
        /// </summary>
        [DefaultValue(false)]
        public bool ShowSlack { get; set; }

        /// <summary>
        /// Get the number of rows in the chart
        /// </summary>
        public int RowCount { get; private set; }

        /// <summary>
        /// Occurs when the mouse is moving over a Task
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskMouseOver = null;

        /// <summary>
        /// Occurs when the mouse leaves a Task
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskMouseOut = null;

        /// <summary>
        /// Occurs when a Task is clicked
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskMouseClick = null;

        /// <summary>
        /// Occurs when a Task is double clicked by the mouse
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskMouseDoubleClick = null;

        /// <summary>
        /// Occurs when a Task is being dragged by the mouse
        /// </summary>
        public event EventHandler<MousePanEventArgs> MousePan = null;

        /// <summary>
        /// Occurs when a Task is being dragged by the mouse
        /// </summary>
        public event EventHandler<TaskDragDropEventArgs> TaskMouseDrag = null;

        /// <summary>
        /// Occurs when a dragged Task is being dropped by releasing any previously pressed mouse button.
        /// </summary>
        public event EventHandler<TaskDragDropEventArgs> TaskMouseDrop = null;

        /// <summary>
        /// Occurs when a task is selected.
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskSelected = null;

        /// <summary>
        /// Occurs before one or more tasks are being deselected. All Task in Chart.SelectedTasks will be deselected.
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskDeselecting = null;

        /// <summary>
        /// Occurs before a Task gets painted
        /// </summary>
        public event EventHandler<TaskPaintEventArgs> PaintTask = null;

        /// <summary>
        /// Occurs before overlays get painted
        /// </summary>
        public event EventHandler<ChartPaintEventArgs> PaintOverlay = null;

        /// <summary>
        /// Occurs before the header gets painted
        /// </summary>
        public event EventHandler<HeaderPaintEventArgs> PaintHeader = null;

        /// <summary>
        /// Occurs before the header date tick mark gets painted
        /// </summary>
        public event EventHandler<TimelinePaintEventArgs> PaintTimeline = null;

        /// <summary>
        /// Get the line number of the specified task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool TryGetRow(Task task, out int row)
        {
            row = 0;
            if (_mChartTaskHitRects.ContainsKey(task))
            {
                // collection contains parts
                row = _ChartCoordToChartRow(_mChartTaskHitRects[task].Top);
                return true;
            }
            else if (_mChartTaskRects.ContainsKey(task))
            {
                // collection contains splits
                row = _ChartCoordToChartRow(_mChartTaskRects[task].Top);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get the task at the specified line number
        /// </summary>
        /// <param name="row"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public bool TryGetTask(int row, out Task task)
        {
            task = null;
            if (row > 0 && row < m_Project.Tasks.Count())
            {
                task = _mChartTaskRects.ElementAtOrDefault(row - 1).Key;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initialize this Chart with a Project
        /// </summary>
        /// <param name="project"></param>
        public void Init(ProjectManager<Task, object> project)
        {
            m_Project = project;
            _GenerateModels();
        }

        /// <summary>
        /// Print the Chart to the specified PrintDocument.
        /// </summary>
        //public void Print(PrintDocument document, float scale = 1.0f)
        //{
        //    // save a copy of the current viewport and swap it with PrintViewport
        //    var viewport = m_Viewport;

        //    float x = 0; // viewport world x, y coords
        //    float y = 0;
        //    int pageCount = 0;

        //    document.PrintPage += (s, e) =>
        //    {
        //        e.HasMorePages = false;
        //        pageCount++;

        //        // create a PrintViewport to navigate the world
        //        var printViewport = new PrintViewport(e.Graphics,
        //            viewport.WorldWidth, viewport.WorldHeight,
        //            e.MarginBounds.Width, e.MarginBounds.Height,
        //            e.PageSettings.Margins.Left, e.PageSettings.Margins.Right)
        //        { Scale = scale };
        //        m_Viewport = printViewport;

        //        // move the viewport
        //        printViewport.X = x;
        //        printViewport.Y = y;

        //        // set clip and draw
        //        e.Graphics.SetClip(e.MarginBounds);
        //        _Draw(e.Graphics, e.PageBounds); // NOT TESTED

        //        // check if reached end of printing
        //        if (printViewport.Rectangle.Right < printViewport.WorldWidth)
        //        {
        //            // continue horizontally
        //            x += printViewport.Rectangle.Width;
        //            e.HasMorePages = true;
        //        }
        //        else
        //        {
        //            // reached end of worldwidth so we go down vertically once
        //            x = 0;
        //            if (printViewport.Rectangle.Bottom < printViewport.WorldHeight)
        //            {
        //                y += printViewport.Rectangle.Height;
        //                e.HasMorePages = true;
        //            }
        //        }
        //    };
        //    document.Print();

        //    // restore the viewport 
        //    m_Viewport = viewport;
        //}

        ///// <summary>
        ///// Print the Chart to an Image
        ///// </summary>
        ///// <param name="scale">Scale to print the image at.</param>
        //public Bitmap Print(float scale = 1.0f)
        //{
        //    var viewport = m_Viewport;
        //    m_Viewport = new ImageViewport(scale, viewport.WorldWidth, viewport.WorldHeight);

        //    Bitmap image = new Bitmap((int)Math.Ceiling(viewport.WorldWidth * scale), (int)Math.Ceiling(viewport.WorldHeight * scale));
        //    var graphics = Graphics.FromImage(image);

        //    _Draw(graphics, Rectangle.Ceiling(m_Viewport.Rectangle));
        //    //_Draw(graphics, Rectangle.Ceiling(new RectangleF(_mViewport.X + barChartStartX, _mViewport.Y, _mViewport.Rectangle.Width, _mViewport.Rectangle.Height)));

        //    m_Viewport = viewport;

        //    return image;
        //}

        /// <summary>
        /// Get information about the chart area at the mouse coordinate of the chart
        /// </summary>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public ChartInfo GetChartInfo(Point mouse)
        {
            var row = _ChartCoordToChartRow(mouse.Y);
            var col = _GetDeviceColumnUnderMouse(mouse);
            var task = _GetTaskUnderMouse(mouse);
            return new ChartInfo(row, _mHeaderInfo.DateTimes[col], task);
        }

        /// <summary>
        /// Set tool tip for the specified task
        /// </summary>
        /// <param name="task"></param>
        /// <param name="text"></param>
        public void SetToolTip(Task task, string text)
        {
            if (task != null && text != string.Empty)
                _mTaskToolTip[task] = text;
        }

        /// <summary>
        /// Get tool tip currently set for the specified task
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public string GetToolTip(Task task)
        {
            if (task != null)
                return _mTaskToolTip[task];
            else
                return string.Empty;
        }

        /// <summary>
        /// Clear tool tip for the specified task
        /// </summary>
        /// <param name="task"></param>
        public void ClearToolTip(Task task)
        {
            if (task != null)
                _mTaskToolTip.Remove(task);
        }

        /// <summary>
        /// Clear all tool tips
        /// </summary>
        public void ClearToolTips()
        {
            _mTaskToolTip.Clear();
        }

        /// <summary>
        /// Scroll to the specified DateTime
        /// </summary>
        /// <param name="datetime"></param>
        public void ScrollTo(DateTime datetime)
        {
            TimeSpan span = datetime - m_Project.Start;
            Viewport.X = GetSpan(span);
        }

        /// <summary>
        /// Scroll to the specified task
        /// </summary>
        /// <param name="task"></param>
        public void ScrollTo(Task task)
        {
            if (_mChartTaskRects.ContainsKey(task))
            {
                var rect = _mChartTaskRects[task];
                Viewport.X = rect.Left - this.MinorWidth;
                Viewport.Y = rect.Top - this.HeaderOneHeight - this.HeaderTwoHeight;
            }
        }

        /// <summary>
        /// Begin billboard mode. Graphics must orginate from Chart and be same as that used in EndBillboardMode.
        /// </summary>
        /// <param name="graphics"></param>
        public void BeginBillboardMode(Graphics graphics)
        {
            graphics.Transform = ControlViewport.Identity;
        }

        /// <summary>
        /// End billboard mode. Graphics must orginate from Chart and be same as that used in BeginBillboardMode.
        /// </summary>
        /// <param name="graphics"></param>
        public void EndBillboardMode(Graphics graphics)
        {
            graphics.Transform = Viewport.Projection;
        }

        /// <summary>
        /// Convert the specified timespan to pixels units of the Chart x-coordinates
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public float GetSpan(TimeSpan span)
        {
            double pixels = 0;
            switch (TimeResolution)
            {
                case TimeResolution.Day:
                    pixels = span.TotalDays * (double)MinorWidth;
                    break;
                case TimeResolution.Week:
                    pixels = span.TotalDays / 7f * (double)MinorWidth;
                    break;
                case TimeResolution.Hour:
                    pixels = span.TotalHours * (double)MinorWidth;
                    break;
            }

            return (float)pixels;
        }

        /// <summary>
        /// Convert the pixel units of the Chart x-coordinates to TimeSpan
        /// </summary>
        /// <param name="dx"></param>
        /// <returns></returns>
        public TimeSpan GetSpan(float dx)
        {

            TimeSpan span = TimeSpan.MinValue;
            switch (TimeResolution)
            {
                case TimeResolution.Day:
                    span = TimeSpan.FromDays(dx / MinorWidth);
                    break;
                case TimeResolution.Week:
                    span = TimeSpan.FromDays(dx / MinorWidth * 7f);
                    break;
                case TimeResolution.Hour:
                    span = TimeSpan.FromHours(dx / MinorWidth);
                    break;
            }
            return span;
        }

        #endregion Public Methods

        #region UserControl Events

        /// <summary>
        /// Raises the System.Windows.Forms.Control.Paint event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!this.DesignMode)
                this._Draw(e.Graphics, e.ClipRectangle);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseMove event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var task = _GetTaskUnderMouse(e.Location);
            var deviceLocation = Viewport.DeviceToWorldCoord(e.Location);


            // Check if mouse is in task list
            // If so, do not do anything
            if (task != null && _mDraggedTask == null)
            {
                if (_isWithinHitBoxEdge(_mChartTaskHitRects[task], deviceLocation, 10))
                    this.Cursor = Cursors.SizeWE;
                else
                    this.Cursor = Cursors.Hand;
            }

            // Hot tracking
            if (_mBarMouseEntered != null && task == null) // Mouse moved out of hitbox
            {
                OnTaskMouseOut(new TaskMouseEventArgs(_mBarMouseEntered, RectangleF.Empty, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                _mBarMouseEntered = null;

            }
            else if (_mBarMouseEntered == null && task != null) // Mouse moved into hitbox
            {
                _mBarMouseEntered = task;
                OnTaskMouseOver(new TaskMouseEventArgs(_mBarMouseEntered, _mChartTaskHitRects[task], e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }

            // Dragging
            if (AllowTaskDragDrop && _mDraggedTask != null)
            {
                this.Cursor = Cursors.SizeWE; // set drag cursor
                Task target = task;
                if (target == _mDraggedTask)
                    target = null;

                RectangleF targetRect = target == null ? RectangleF.Empty : _mChartTaskHitRects[target];
                int row = _DeviceCoordToChartRow(e.Location.Y);
                OnTaskMouseDrag(new TaskDragDropEventArgs(_mDragTaskStartLocation, _mDragTaskLastLocation, _mDraggedTask, _mChartTaskHitRects[_mDraggedTask], target, targetRect, row, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                _mDragTaskLastLocation = e.Location;
            }

            // Panning mode
            if (_mDraggedTask == null && e.Button == MouseButtons.Middle)
            {
                this.Cursor = Cursors.SizeAll;
                float distanceY = e.Y - _mPanViewLastLocation.Y;

                Viewport.X -= e.X - _mPanViewLastLocation.X;
                Viewport.Y -= distanceY;
                _mPanViewLastLocation = e.Location;

                this.Invalidate();

                //// Call scroll event
                //if(distanceY < 0)
                //    OnScroll(new ScrollEventArgs(ScrollEventType.SmallDecrement, (int) distanceY, ScrollOrientation.VerticalScroll));
                //else
                //    OnScroll(new ScrollEventArgs(ScrollEventType.SmallIncrement, (int) distanceY, ScrollOrientation.VerticalScroll));
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseClick event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            var deviceLocation = Viewport.DeviceToWorldCoord(e.Location);

            var task = _GetTaskUnderMouse(e.Location);

            if (task != null)
            {
                OnTaskMouseClick(new TaskMouseEventArgs(task, _mChartTaskHitRects[task], e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }
            else
            {
                OnTaskDeselecting(new TaskMouseEventArgs(task, RectangleF.Empty, e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }

            base.OnMouseClick(e);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseDown event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Begin Drag
            _mDragTaskStartLocation = e.Location;
            _mDragTaskLastLocation = e.Location;
            _mPanViewStartLocation = e.Location;
            _mPanViewLastLocation = e.Location;

            if (AllowTaskDragDrop)
            {
                _mDraggedTask = _GetTaskUnderMouse(e.Location);
                //if (_mDragSource != null)
                //{
                //    _mDragStartLocation = e.Location;
                //    _mDragLastLocation = e.Location;
                //}

                if (_mDraggedTask != null)
                {
                    PointF point = Viewport.DeviceToWorldCoord(e.Location);
                    RectangleF rect = _mChartTaskHitRects[_mDraggedTask];
                    _IsNearEdge = _isWithinHitBoxEdge(rect, point, 10);
                    _IsDragStart = rect.Left + 10 > point.X;
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseUp event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            // reset cursor to handle end of panning mode;
            this.Cursor = Cursors.Default;

            // Complete mouse panning
            if (_mPanViewStartLocation != _mPanViewLastLocation)
            {
                OnMousePan(new MousePanEventArgs(_mPanViewStartLocation, _mPanViewLastLocation, e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }

            // Drop task
            if (AllowTaskDragDrop && _mDraggedTask != null)
            {
                var target = _GetTaskUnderMouse(e.Location);
                if (target == _mDraggedTask) target = null;
                var targetRect = target == null ? RectangleF.Empty : _mChartTaskHitRects[target];
                int row = _DeviceCoordToChartRow(e.Location.Y);
                OnTaskMouseDrop(new TaskDragDropEventArgs(_mDragTaskStartLocation, _mDragTaskLastLocation, _mDraggedTask, _mChartTaskHitRects[_mDraggedTask], target, targetRect, row, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                _mDraggedTask = null;
                _IsNearEdge = false;
                _mDragTaskLastLocation = Point.Empty;
                _mDragTaskStartLocation = Point.Empty;
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseDoubleClick event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var deviceLocation = Viewport.DeviceToWorldCoord(e.Location);

            var task = _GetTaskUnderMouse(e.Location);
            if (task != null)
            {
                OnTaskMouseDoubleClick(new TaskMouseEventArgs(task, _mChartTaskHitRects[task], e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }

            base.OnMouseDoubleClick(e);
        }

        private void GanttChart_OnScroll(object sender, ScrollEventArgs e)
        {
            OnScroll(e);
        }

        #endregion UserControl Events

        #region Chart Events

        /// <summary>
        /// Raises the TaskMouseOver event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseOver(TaskMouseEventArgs e)
        {
            TaskMouseOver?.Invoke(this, e);

            this.Cursor = Cursors.Hand;

            var task = e.Task;
            if (m_Project.IsPart(e.Task)) task = m_Project.SplitTaskOf(task);
            if (_mTaskToolTip.ContainsKey(task))
            {
                _mOverlay.ShowToolTip(Viewport.DeviceToWorldCoord(e.Location), _mTaskToolTip[task]);
                this.Invalidate();
            }
        }

        /// <summary>
        /// Raises the TaskMouseOut event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseOut(TaskMouseEventArgs e)
        {
            TaskMouseOut?.Invoke(this, e);

            this.Cursor = Cursors.Default;

            _mOverlay.HideToolTip();
            this.Invalidate();
        }

        /// <summary>
        /// Raises the OnMousePan event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnMousePan(MousePanEventArgs e)
        {
            MousePan?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the TaskMouseDrag event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseDrag(TaskDragDropEventArgs e)
        {
            // fire listeners
            TaskMouseDrag?.Invoke(this, e);

            // Default drag behaviors **********************************
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                if (m_Project.IsPart(e.Source))
                {
                    // Use delay of split task
                    var complete = e.Source.Complete + (float)(e.X - e.PreviousLocation.X) / GetSpan(e.Source.Duration + m_Project.SplitTaskOf(e.Source).Delay);
                    m_Project.SetComplete(e.Source, complete);

                }
                else
                {
                    var complete = e.Source.Complete + (float)(e.X - e.PreviousLocation.X) / GetSpan(e.Source.Duration + e.Source.Delay);
                    m_Project.SetComplete(e.Source, complete);
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right || e.Button == MouseButtons.Left && _IsNearEdge)
            {
                if (e.Target == null)
                {
                    int delta = (e.PreviousLocation.X - e.StartLocation.X);

                    _mOverlay.DraggedRect = e.SourceRect;

                    if (_IsDragStart)
                    {
                        _mOverlay.DraggedRect.X += delta;
                        _mOverlay.DraggedRect.Width -= delta;
                    }
                    else
                    {
                        _mOverlay.DraggedRect.Width += delta;
                    }

                }
                else // drop targetting (join)
                {
                    _mOverlay.DraggedRect = e.TargetRect;
                    _mOverlay.Row = int.MinValue;
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                _mOverlay.Clear();

                if (e.Target == null)
                {
                    if (Control.ModifierKeys.HasFlag(Keys.Shift))
                    {
                        // insertion line
                        _mOverlay.Row = e.Row;
                    }
                    else
                    {
                        // displacing horizontally
                        _mOverlay.DraggedRect = e.SourceRect;
                        _mOverlay.DraggedRect.Offset((e.X - e.StartLocation.X), 0);
                    }
                }
                else // drop targetting (subtask / predecessor)
                {
                    _mOverlay.DraggedRect = e.TargetRect;
                    _mOverlay.Row = int.MinValue;
                }
            }
            this.Invalidate();
        }
        /// <summary>
        /// Raises the TaskMouseDrop event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseDrop(TaskDragDropEventArgs e)
        {
            // Fire event
            TaskMouseDrop?.Invoke(this, e);

            var delta = (e.PreviousLocation.X - e.StartLocation.X);

            if (e.Button == System.Windows.Forms.MouseButtons.Left && !_IsNearEdge)
            {
                if (e.Target == null)
                {
                    if (Control.ModifierKeys.HasFlag(Keys.Shift))
                    {
                        // insert
                        Task source = e.Source;
                        if (m_Project.IsPart(source)) source = m_Project.SplitTaskOf(source);
                        int from;
                        if (this.TryGetRow(source, out from))
                            m_Project.Move(source, e.Row - from);
                    }
                    else
                    {
                        // displace horizontally
                        var start = e.Source.Start + GetSpan(delta);
                        m_Project.SetStart(e.Source, start);
                    }
                }
                else // have drop target
                {
                    if (Control.ModifierKeys.HasFlag(Keys.Shift))
                    {
                        m_Project.Relate(e.Target, e.Source);
                    }
                    else if (Control.ModifierKeys.HasFlag(Keys.Alt))
                    {
                        var source = e.Source;
                        if (m_Project.IsPart(source)) source = m_Project.SplitTaskOf(source);
                        if (m_Project.DirectGroupOf(source) == e.Target)
                        {
                            m_Project.Ungroup(e.Target, e.Source);
                        }
                        else
                        {
                            m_Project.Unrelate(e.Target, source);
                        }
                    }
                    else
                    {
                        m_Project.Group(e.Target, e.Source);
                    }
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right || e.Button == MouseButtons.Left && _IsNearEdge)
            {
                if (e.Target == null)
                {
                    TimeSpan duration;

                    if (_IsDragStart)
                    {
                        TimeSpan diff = GetSpan(delta);
                        m_Project.SetStart(e.Source, e.Source.Start + diff);
                        m_Project.SetDuration(e.Source, e.Source.Duration - diff);
                    }
                    else
                    {
                        duration = e.Source.Duration + GetSpan(delta);
                        m_Project.SetDuration(e.Source, duration);
                    }
                }
                else // have target then we do a join
                {
                    m_Project.Join(e.Target, e.Source);
                }
            }

            _mOverlay.Clear();
            this.Invalidate();
        }
        /// <summary>
        /// Raises the TaskMouseClick event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseClick(TaskMouseEventArgs e)
        {
            TaskMouseClick?.Invoke(this, e);

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (ModifierKeys.HasFlag(Keys.Shift)) // activate multi-select
                {
                    if (!_mSelectedTasks.Remove(e.Task))
                    {
                        _mSelectedTasks.Add(e.Task);
                    }
                }
                else
                {
                    OnTaskDeselecting(e);
                    _mSelectedTasks.Add(e.Task);
                }
                OnTaskSelected(e);
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                if (ModifierKeys.HasFlag(Keys.Shift))
                {
                    var newtask = CreateTaskDelegate();
                    m_Project.Add(newtask);
                    m_Project.SetStart(newtask, e.Task.Start);
                    m_Project.SetDuration(newtask, new TimeSpan(5, 0, 0, 0));
                    if (m_Project.IsPart(e.Task)) m_Project.Move(newtask, m_Project.IndexOf(m_Project.SplitTaskOf(e.Task)) + 1 - m_Project.IndexOf(newtask));
                    else m_Project.Move(newtask, m_Project.IndexOf(e.Task) + 1 - m_Project.IndexOf(newtask));
                }
                else if (Control.ModifierKeys.HasFlag(Keys.Alt))
                    m_Project.Delete(e.Task);
            }
            this.Invalidate();
        }

        /// <summary>
        /// Raises the TaskMouseDoubleClick event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseDoubleClick(TaskMouseEventArgs e)
        {
            TaskMouseDoubleClick?.Invoke(this, e);

            //if (e.Button == System.Windows.Forms.MouseButtons.Left) // Handled in UI instead
            //{
            //    //e.Task.IsCollapsed = !e.Task.IsCollapsed;
            //}
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                TimeSpan duration = GetSpan(Viewport.DeviceToWorldCoord(e.Location).X - e.Rectangle.Left);
                if (m_Project.IsPart(e.Task)) m_Project.Split(e.Task, CreateTaskDelegate(), duration);
                else m_Project.Split(e.Task, CreateTaskDelegate(), CreateTaskDelegate(), duration);
            }

            this.Invalidate();
        }

        /// <summary>
        /// Raises the TaskSelected event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskSelected(TaskMouseEventArgs e)
        {
            TaskSelected?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the TaskDeselecting event and then clear all the selected tasks
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskDeselecting(TaskMouseEventArgs e)
        {
            TaskDeselecting?.Invoke(this, e);

            // deselect all tasks
            _mSelectedTasks.Clear();
        }
        /// <summary>
        /// Raises the PaintOverlay event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintOverlay(ChartPaintEventArgs e)
        {
            if (this.PaintOverlay != null)
                PaintOverlay(this, e);
        }
        /// <summary>
        /// Raises the PaintTickMark event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintTimeline(TimelinePaintEventArgs e)
        {
            PaintTimeline?.Invoke(this, e);
        }
        /// <summary>
        /// Raises the PaintHeader event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPaintHeader(HeaderPaintEventArgs e)
        {
            PaintHeader?.Invoke(this, e);
        }

        #endregion Chart Events

        #region OverlayPainter
        private ChartOverlay _mOverlay = new ChartOverlay();
        class ChartOverlay
        {
            public void Paint(ChartPaintEventArgs e)
            {
                var g = e.Graphics;
                var chart = e.Chart;

                // dragging outline / trail
                if (DraggedRect != RectangleF.Empty)
                    g.DrawRectangle(Pens.Red, DraggedRect);

                // insertion indicator line
                if (Row != int.MinValue)
                {
                    float y = e.Chart._ChartRowToChartCoord(Row) + e.Chart.BarSpacing / 2.0f;
                    g.DrawLine(Pens.CornflowerBlue, new PointF(0, y), new PointF(e.Chart.Width, y));
                }

                // tool tip
                if (_mToolTipMouse != Point.Empty && _mToolTipText != string.Empty)
                {
                    var size = g.MeasureString(_mToolTipText, chart.Font).ToSize();
                    var tooltiprect = new RectangleF(_mToolTipMouse, size);
                    tooltiprect.Offset(0, -tooltiprect.Height);
                    var textstart = new PointF(tooltiprect.Left, tooltiprect.Top);
                    tooltiprect.Inflate(5, 5);
                    g.FillRectangle(Brushes.LightYellow, tooltiprect);
                    g.DrawString(_mToolTipText, chart.Font, Brushes.Black, textstart);
                }
            }

            public void ShowToolTip(PointF worldcoord, string text)
            {
                _mToolTipMouse = worldcoord;
                _mToolTipText = text;
            }

            public void HideToolTip()
            {
                _mToolTipMouse = Point.Empty;
                _mToolTipText = string.Empty;
            }

            public void Clear()
            {
                DraggedRect = RectangleF.Empty;
                Row = int.MinValue;
            }

            private PointF _mToolTipMouse = PointF.Empty;
            private string _mToolTipText = string.Empty;
            public RectangleF DraggedRect = RectangleF.Empty;
            public int Row = int.MinValue;
        }
        #endregion

        #region Private Helper Methods

        private Task _GetTaskUnderMouse(Point mouse)
        {
            var chartcoord = Viewport.DeviceToWorldCoord(mouse);

            if (!_mHeaderInfo.H1Rect.Contains(chartcoord)
                && !_mHeaderInfo.H2Rect.Contains(chartcoord))
            {
                foreach (var task in _mChartTaskHitRects.Keys)
                {
                    if (_mChartTaskHitRects[task].Contains(chartcoord))
                        return task;
                }
            }

            return null;
        }

        private int _GetDeviceColumnUnderMouse(Point mouse)
        {
            var worldcoord = Viewport.DeviceToWorldCoord(mouse);

            return _mHeaderInfo.Columns.Select((x, i) => new { x, i }).FirstOrDefault(x => x.x.Contains(worldcoord)).i;
        }

        /// <summary>
        /// Convert view Y coordinate to zero based row number
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private int _DeviceCoordToChartRow(float y)
        {
            y = Viewport.DeviceToWorldCoord(new PointF(0, y)).Y;
            var row = (int)((y - this.BarSpacing - this.HeaderOneHeight) / this.BarSpacing);
            return row < 0 ? 0 : row;
        }

        /// <summary>
        /// Convert world Y coordinate to zero-based row number
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        private int _ChartCoordToChartRow(float y)
        {
            var row = (int)((y - this.HeaderTwoHeight - this.HeaderOneHeight) / this.BarSpacing);
            return row < 0 ? 0 : row;
        }

        /// <summary>
        /// Convert zero based row number to client Y coordinates
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private float _ChartRowToChartCoord(int row)
        {
            return row * this.BarSpacing + this.HeaderTwoHeight + this.HeaderOneHeight;
        }

        /// <summary>
        /// Draw the Chart using the specified graphics. Only object within the clipRect are drawn, the rest are culled away.
        /// </summary>
        private void _Draw(Graphics graphics, Rectangle clipRect)
        {
            graphics.Clear(Color.White);

            int row = 0;
            if (m_Project != null)
            {
                // Set model view matrix
                graphics.Transform = Viewport.Projection;

                // Generate rectangles
                _GenerateModels();
                _GenerateHeaders();

                // Draw columns in the background
                _DrawColumns(graphics);

                // Draw predecessor arrows
                if (ShowRelations) _DrawPredecessorLines(graphics);

                // Draw bar charts
                row = _DrawTasks(graphics, clipRect);

                // Draw the header
                _DrawHeader(graphics, clipRect);

                // Paint overlays
                ChartPaintEventArgs paintargs = new ChartPaintEventArgs(graphics, clipRect, this);
                OnPaintOverlay(paintargs);
                _mOverlay.Paint(paintargs);
            }
            else
            {
                // Nothing to draw
            }

            //graphics.DrawRectangle(this.HeaderFormat.Border, new RectangleF(Viewport.X, this.HeaderTwoHeight + this.HeaderOneHeight, 100f, 32f));
            //graphics.DrawRectangle(this.HeaderFormat.Border, new RectangleF(Viewport.X, 1006 * BarSpacing + HeaderOneHeight + HeaderTwoHeight, 100f, 32f));

            // Flush
            graphics.Flush();
        }

        /// <summary>
        /// Generate the task models and resize the world accordingly
        /// </summary>
        private void _GenerateModels()
        {
            // Clear Models
            _mChartTaskRects.Clear();
            _mChartTaskHitRects.Clear();
            _mChartSlackRects.Clear();
            _mChartDelayRects.Clear();
            _mChartTaskPartRects.Clear();

            var pHeight = this.Parent == null ? this.Height : this.Parent.Height;
            var pWidth = this.Parent == null ? this.Width : this.Parent.Width;

            // loop over the tasks and pick up items
            var end = TimeSpan.MinValue;
            int row = 0;
            foreach (var task in m_Project.Tasks)
            {
                if (!m_Project.GroupsOf(task).Any(x => x.IsCollapsed) && !task.IsFiltered)
                {
                    int yCoord = row * this.BarSpacing + this.HeaderTwoHeight + this.HeaderOneHeight + (this.BarSpacing - this.BarHeight) / 2;
                    RectangleF taskRect;

                    // Compute task rectangle
                    taskRect = new RectangleF(GetSpan(task.Start), yCoord, GetSpan(task.Duration + task.Delay), this.BarHeight);
                    _mChartTaskRects.Add(task, taskRect); // also add groups and split tasks (not just task parts)

                    if (!m_Project.IsSplit(task))
                    {
                        // Add normal Task Rectangles to hitRect collection for hit testing
                        _mChartTaskHitRects.Add(task, taskRect);
                    }
                    else // Compute task part rectangles if task is a split task
                    {
                        var parts = new List<KeyValuePair<Task, RectangleF>>();
                        _mChartTaskPartRects.Add(task, parts);

                        var taskParts = m_Project.PartsOf(task);
                        for (int i = 0; i < taskParts.Count() - 1; i++)
                        {
                            var part = taskParts.ElementAt(i);

                            taskRect = new RectangleF(GetSpan(part.Start), yCoord, GetSpan(part.Duration), this.BarHeight); // parts do not implement delay
                            parts.Add(new KeyValuePair<Task, RectangleF>(part, taskRect));

                            // Parts are mouse enabled, add to hitRect collection
                            _mChartTaskHitRects.Add(part, taskRect);
                        }

                        // add delay as part of split task
                        var lastPart = taskParts.Last();

                        var delayRect = new RectangleF(GetSpan(lastPart.Start), yCoord, GetSpan(lastPart.Duration + task.Delay), this.BarHeight);
                        parts.Add(new KeyValuePair<Task, RectangleF>(lastPart, delayRect));
                        _mChartTaskHitRects.Add(lastPart, delayRect);
                    }
                    float xCoord = GetSpan(task.End);

                    // Compute Delay Rectangles
                    if (task.ActualEnd != task.End)
                    {
                        float span = GetSpan(task.Delay);

                        var delayRect = new RectangleF(xCoord, yCoord, span, this.BarHeight);
                        _mChartDelayRects.Add(task, delayRect);

                        xCoord += span;
                    }

                    // Compute Slack Rectangles
                    if (this.ShowSlack)
                    {
                        var slackRect = new RectangleF(xCoord, yCoord, GetSpan(task.Slack), this.BarHeight);
                        _mChartSlackRects.Add(task, slackRect);
                    }

                    // Find maximum end time
                    if (task.End > end) end = task.End;

                    row++;
                }
            }

            // Update for listtask
            RowCount = row;

            //Viewport.WorldHeight = Math.Max(pHeight, row * this.BarSpacing + this.BarHeight);
            Viewport.WorldHeight = Math.Max(pHeight, (row + 1) * BarSpacing + HeaderOneHeight + HeaderTwoHeight + Viewport.HorizontalScroll.ClientSize.Height); // LAZYFIX: Add single row to world height to keep sync with task_list when scrolled to the bottom
            Viewport.WorldWidth = Math.Max(pWidth, GetSpan(end) + 200);
        }

        /// <summary>
        /// Generate Header rectangles and dates
        /// </summary>
        private void _GenerateHeaders()
        {
            // only generate the necessary headers by determining the current viewport location
            var h1Rect = new RectangleF(Viewport.X, Viewport.Y, Viewport.Rectangle.Width, this.HeaderOneHeight);
            var h2Rect = new RectangleF(h1Rect.Left, h1Rect.Bottom, Viewport.Rectangle.Width, this.HeaderTwoHeight);
            var labelRects = new List<RectangleF>();
            var columns = new List<RectangleF>();
            var datetimes = new List<DateTime>();

            // generate columns across the viewport area           
            var minorDate = __CalculateViewportStart(); // start date of chart
            var minorInterval = GetSpan(MinorWidth);
            // calculate coordinates of rectangles
            var labelRect_Y = Viewport.Y + this.HeaderOneHeight;
            var labelRect_X = (int)(Viewport.X / MinorWidth) * MinorWidth;
            var columns_Y = labelRect_Y + this.HeaderTwoHeight;

            // From second column onwards,
            // loop over the number of <TimeScaleDisplay> each with width of MajorWidth,
            // creating the Major and Minor header rects and generating respective date time information
            while (labelRect_X < Viewport.Rectangle.Right) // keep creating H1 labels until we are out of the viewport
            {
                datetimes.Add(minorDate);
                labelRects.Add(new RectangleF(labelRect_X, labelRect_Y, MinorWidth, HeaderTwoHeight));
                columns.Add(new RectangleF(labelRect_X, columns_Y, MinorWidth, Viewport.Rectangle.Height));
                minorDate += minorInterval;
                labelRect_X += MinorWidth;
            }

            _mHeaderInfo.H1Rect = h1Rect;
            _mHeaderInfo.H2Rect = h2Rect;
            _mHeaderInfo.LabelRects = labelRects;
            _mHeaderInfo.Columns = columns;
            _mHeaderInfo.DateTimes = datetimes;
        }

        /// <summary>
        /// Calculate the date in the first visible column in the viewport
        /// </summary>
        /// <returns></returns>
        private DateTime __CalculateViewportStart()
        {
            float vpTime = (int)(Viewport.X / this.MinorWidth);
            if (this.TimeResolution == Edcore.GanttChart.TimeResolution.Week)
            {
                return m_Project.Start.AddDays(vpTime * 7);
            }
            else if (this.TimeResolution == Edcore.GanttChart.TimeResolution.Day)
            {
                return m_Project.Start.AddDays(vpTime);
            }
            else if (this.TimeResolution == Edcore.GanttChart.TimeResolution.Hour)
            {
                return m_Project.Start.AddHours(vpTime);
            }
            else if (this.TimeResolution == TimeResolution.Minute)
            {
                return m_Project.Start.AddMinutes(vpTime);
            }

            throw new NotImplementedException("Unable to determine TimeResolution.");
        }

        private void _DrawColumns(Graphics graphics)
        {
            // draw column lines
            graphics.DrawRectangles(this.HeaderFormat.Border, _mHeaderInfo.Columns.ToArray());

            // fill weekend columns
            for (int i = 0; i < _mHeaderInfo.DateTimes.Count; i++)
            {
                var date = _mHeaderInfo.DateTimes[i];
                // highlight weekends for day time scale
                if (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday)
                {
                    var pattern = new HatchBrush(HatchStyle.Percent20, this.HeaderFormat.Border.Color, Color.Transparent);
                    graphics.FillRectangle(pattern, _mHeaderInfo.Columns[i]);
                }
            }
        }

        private void _DrawHeader(Graphics graphics, Rectangle clipRect)
        {
            var info = _mHeaderInfo;
            var viewRect = Viewport.Rectangle;

            // Draw header backgrounds
            var e = new HeaderPaintEventArgs(graphics, clipRect, this, this.Font, this.HeaderFormat);
            OnPaintHeader(e);
            var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(info.H1Rect, e.Format.GradientLight, e.Format.GradientDark, System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            graphics.FillRectangles(gradient, new RectangleF[] { info.H1Rect, info.H2Rect });
            graphics.DrawRectangles(e.Format.Border, new RectangleF[] { info.H1Rect, info.H2Rect });

            // Draw the header scales
            __DrawScale(graphics, clipRect, e.Font, e.Format, info.LabelRects, info.DateTimes);

            // draw "Now" line
            float xf = GetSpan(m_Project.Now);
            var pen = new Pen(e.Format.Border.Color) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            graphics.DrawLine(pen, new PointF(xf, Viewport.Y), new PointF(xf, Viewport.Rectangle.Bottom));
        }

        private void __DrawScale(Graphics graphics, Rectangle clipRect, Font font, HeaderFormat headerformat, List<RectangleF> labelRects, List<DateTime> dates)
        {
            TimelinePaintEventArgs e = null;
            DateTime datetime = dates[0]; // these initialisation values matter
            DateTime datetimeprev = dates[0]; // these initialisation values matter
            for (int i = 0; i < labelRects.Count; i++)
            {
                // Give user a chance to format the tickmark that is to be drawn
                // https://blog.nicholasrogoff.com/2012/05/05/c-datetime-tostring-formats-quick-reference/
                datetime = dates[i];
                LabelFormat minor;
                LabelFormat major;
                ___GetLabelFormat(datetime, datetimeprev, out minor, out major);
                e = new TimelinePaintEventArgs(graphics, clipRect, this, datetime, datetimeprev, minor, major);
                OnPaintTimeline(e);

                // Draw the label if not already handled by the user
                if (!e.Handled)
                {
                    if (!string.IsNullOrEmpty(minor.Text))
                    {
                        // Draw minor label
                        var textbox = graphics.TextBoxAlign(minor.Text, minor.TextAlign, minor.Font, labelRects[i], minor.Margin);
                        graphics.DrawString(minor.Text, minor.Font, minor.Color, textbox);
                    }

                    if (!string.IsNullOrEmpty(major.Text))
                    {
                        // Draw major label
                        var majorLabelRect = new RectangleF(labelRects[i].X, Viewport.Y, this.MajorWidth, this.HeaderOneHeight);
                        var textbox = graphics.TextBoxAlign(major.Text, major.TextAlign, major.Font, majorLabelRect, major.Margin);
                        graphics.DrawString(major.Text, major.Font, major.Color, textbox);
                        __DrawMarker(graphics, labelRects[i].X + MinorWidth / 2f, Viewport.Y + HeaderOneHeight - 2f);
                    }
                }

                // Draw dividers for minor scale
                float y1 = Viewport.Y + HeaderOneHeight;
                float y2 = y1 + HeaderTwoHeight;
                foreach (RectangleF rect in _mHeaderInfo.Columns)
                {
                    graphics.DrawLine(this.HeaderFormat.Border, rect.Left, y1, rect.Left, y2);
                }

                // set prev datetime
                datetimeprev = datetime;
            }
        }

        private void ___GetLabelFormat(DateTime datetime, DateTime datetimeprev, out LabelFormat minor, out LabelFormat major)
        {
            minor = new LabelFormat() { Text = string.Empty, Font = this.Font, Color = HeaderFormat.Color, Margin = 3, TextAlign = ChartTextAlign.MiddleCenter };
            major = new LabelFormat() { Text = string.Empty, Font = this.Font, Color = HeaderFormat.Color, Margin = 3, TextAlign = ChartTextAlign.MiddleLeft };

            System.Globalization.GregorianCalendar calendar = new System.Globalization.GregorianCalendar();
            switch (TimeResolution)
            {
                case TimeResolution.Week:
                    minor.Text = calendar.GetWeekOfYear(datetime, System.Globalization.CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday).ToString();
                    if (datetime.Month != datetimeprev.Month) major.Text = datetime.ToString("MMMM");
                    break;
                case TimeResolution.Hour:
                    minor.Text = datetime.Hour.ToString();
                    if (datetime.Day != datetimeprev.Day) major.Text = datetime.ToString("yyyy.MM.dd");
                    break;
                default: // case TimeResolution.Day: -- to implement other TimeResolutions, add to this function or listen to the the PaintTimeline event
                         //minor.Text = ShortDays[datetime.DayOfWeek]; // datetime.ToString("dddd").Substring(0, 1).ToUpper();
                    minor.Text = datetime.ToString("dd");
                    if (datetime.DayOfWeek == DayOfWeek.Sunday) major.Text = datetime.ToString("yyyy.MM.dd");
                    break;
            }
        }

        private void __DrawMarker(Graphics graphics, float offsetX, float offsetY)
        {
            var marker = _Marker.Select(p => new PointF(p.X + offsetX, p.Y + offsetY)).ToArray();
            graphics.FillPolygon(Brushes.LightGoldenrodYellow, marker);
            graphics.DrawPolygon(new Pen(SystemColors.ButtonShadow), marker);
        }

        private int _DrawTasks(Graphics graphics, Rectangle clipRect)
        {
            var viewRect = Viewport.Rectangle;
            int row = 0;
            var crit_task_set = new HashSet<Task>(m_Project.CriticalPaths.SelectMany(x => x));
            var pen = new Pen(Color.Gray);
            float labelMargin = this.MinorWidth / 2.0f + 3.0f;
            pen.DashStyle = DashStyle.Dot;
            TaskPaintEventArgs e;
            foreach (var task in _mChartTaskRects.Keys)
            {
                // Get the taskrect
                var taskRect = _mChartTaskRects[task];
                var delayRect = RectangleF.Empty;
                if (_mChartDelayRects.ContainsKey(task))
                {
                    delayRect = _mChartDelayRects[task];
                }

                // Only begin drawing when the taskrect is to the left of the clipRect's right edge
                if (taskRect.Left <= viewRect.Right)
                {
                    // Crtical Path
                    bool critical = crit_task_set.Contains(task);
                    if (critical) e = new TaskPaintEventArgs(graphics, clipRect, this, task, row, critical, this.Font, this.CriticalTaskFormat);
                    else e = new TaskPaintEventArgs(graphics, clipRect, this, task, row, critical, this.Font, this.TaskFormat);
                    PaintTask?.Invoke(this, e);

                    if (viewRect.IntersectsWith(taskRect))
                    {
                        if (m_Project.IsSplit(task))
                        {
                            __DrawTaskParts(graphics, e, task, pen);
                        }
                        else
                        {
                            __DrawRegularTaskAndGroup(graphics, e, task, taskRect, delayRect);
                        }
                    }

                    // Write completion % text
                    if (showCompletionLabels)
                    {
                        var completion = (int)(task.Complete * 100) + "% complete";
                        var compRect = graphics.TextBoxAlign(completion, ChartTextAlign.MiddleLeft, e.Font, taskRect, labelMargin);
                        compRect.Offset(taskRect.Width, 0);
                        if (viewRect.IntersectsWith(compRect))
                        {
                            graphics.DrawString(completion, e.Font, e.Format.Color, compRect);
                        }
                    }

                    // Draw slack
                    if (this.ShowSlack && task.Complete < 1.0f)
                    {
                        var slackrect = _mChartSlackRects[task];
                        if (viewRect.IntersectsWith(slackrect))
                            graphics.FillRectangle(e.Format.SlackFill, slackrect);
                    }
                }

                row++;
            }

            return row;
        }

        /// <summary>
        /// Only draw lines for all Precedents which are visible on the chart
        /// TODO: draw lines for all collapsed Groups which has precendents
        /// TODO: draw lines for all collapsed Groups which has dependants
        /// </summary>
        /// <param name="graphics"></param>
        private void _DrawPredecessorLines(Graphics graphics)
        {
            var viewRect = Viewport.Rectangle;
            RectangleF clipRectF = new RectangleF(viewRect.X, viewRect.Y, viewRect.Width, viewRect.Height);
            foreach (var precedent in m_Project.Precedents)
            {
                if (!precedent.IsFiltered)
                {
                    foreach (var dependant in m_Project.DirectDependantsOf(precedent))
                    {
                        if (!dependant.IsFiltered)
                        {
                            var pvisible = _mChartTaskRects.ContainsKey(precedent);
                            var dvisible = _mChartTaskRects.ContainsKey(dependant);
                            RectangleF prect, drect;
                            PointF p1, p2, p3;
                            bool isPointingDown;

                            // case where both precedent and dependant are visible, just connect line between them
                            if (!pvisible && !dvisible)
                            {
                                continue; //next dependant please!
                            }
                            else if (pvisible && dvisible)
                            {
                                prect = _mChartTaskRects[precedent];
                                drect = _mChartTaskRects[dependant];

                                // plot and draw lines
                                p1 = new PointF(prect.Right, prect.Top + prect.Height / 2.0f);
                                p2 = new PointF(drect.Left, p1.Y);
                                isPointingDown = p1.Y < drect.Top;
                                p3 = new PointF(drect.Left, isPointingDown ? drect.Top : drect.Bottom);

                            }
                            else if (pvisible && !dvisible)
                            {
                                prect = _mChartTaskRects[precedent];
                                var group = m_Project.GroupsOf(dependant).Last(g => g.IsCollapsed);
                                drect = _mChartTaskRects[group];

                                // if precendent.start > group.start, need to handle this case of line bending back
                                p1 = new PointF(prect.Right, prect.Top + prect.Height / 2.0f);
                                p2 = new PointF(GetSpan(dependant.Start), p1.Y);
                                isPointingDown = p1.Y < drect.Top;
                                p3 = new PointF(GetSpan(dependant.Start), isPointingDown ? drect.Top : drect.Bottom);
                            }
                            else // if(!pvisible && dvisible)
                            {
                                var group = m_Project.GroupsOf(precedent).Last(g => g.IsCollapsed);
                                prect = _mChartTaskRects[group];
                                drect = _mChartTaskRects[dependant];

                                // TODO: if group.end > dependant.start, need to handle this case of line bending back
                                p1 = new PointF(GetSpan(precedent.End), prect.Top + prect.Height / 2.0f);
                                p2 = new PointF(drect.Left, p1.Y);
                                isPointingDown = p1.Y < drect.Top;
                                p3 = new PointF(drect.Left, isPointingDown ? drect.Top : drect.Bottom);
                            }

                            // prepare and draw the lines
                            var size = new SizeF(Math.Abs(p3.X - p1.X), Math.Abs(p3.Y - p1.Y));
                            var linerect = p1.Y < p3.Y ? new RectangleF(p1, size) : new RectangleF(new PointF(p1.X, p1.Y - size.Height), size);
                            if (clipRectF.IntersectsWith(linerect))
                            {
                                graphics.DrawLines(Pens.Black, new PointF[] { p1, p2, p3 });
                                // draw arrowhead
                                var p4 = new PointF(p3.X - 3f, p3.Y + (isPointingDown ? -6f : 6f));
                                var p5 = new PointF(p3.X + 3f, p4.Y);
                                graphics.FillPolygon(Brushes.Black, new PointF[] { p3, p4, p5 });
                            }
                        }
                    }
                }
            }
        }

        private void __DrawRegularTaskAndGroup(Graphics graphics, TaskPaintEventArgs e, Task task, RectangleF taskRect, RectangleF delayRect)
        {
            var fill = taskRect;
            var delayFill = delayRect;
            fill.Width = (int)(fill.Width * task.Complete);

            // Calculate delayed complete rect
            if (fill.Right > delayFill.Left)
            {
                delayFill.Width = fill.Right - delayFill.Left;
            }
            else
            {
                delayFill = Rectangle.Empty;
            }

            // check if this is a parent task / group task, then draw the bracket
            if (m_Project.IsGroup(task))
            {
                if(SmartView)
                {
                    // smart view - displays delay of group only when all tasks at specific time are delayed
                    List<RectangleF> nonDelayRectangles = new List<RectangleF>();
                    List<RectangleF> delayRectangles = new List<RectangleF>();

                    graphics.FillRectangle(e.Format.BackFill, _mChartTaskRects[task]);
                    graphics.DrawRectangle(e.Format.Border, _mChartTaskRects[task]);

                    foreach (Task member in m_Project.MembersOf(task))
                    {
                        nonDelayRectangles.Add(new RectangleF(GetSpan(member.Start), taskRect.Top, GetSpan(member.Duration), taskRect.Height));
                        delayRectangles.Add(new RectangleF(GetSpan(member.End), taskRect.Top, GetSpan(member.Delay), taskRect.Height));
                    }
                    
                    // Remove space covered by task
                    for (int i = 0; i < delayRectangles.Count; i++)
                    {
                        RectangleF registeredRect = delayRectangles.ElementAt(i);
                        
                        foreach(RectangleF removeRect in nonDelayRectangles)
                        {
                            if (registeredRect.X > removeRect.X && registeredRect.Width < removeRect.Width)
                            {
                                // Remove whole registeredRect
                                delayRectangles.RemoveAt(i);
                                break;
                            }
                            if (registeredRect.IntersectsWith(removeRect))
                            {
                                registeredRect.X = Math.Max(registeredRect.X, removeRect.X);
                                float endX = Math.Min(registeredRect.X + registeredRect.Width, removeRect.X + removeRect.Width);
                                registeredRect.Width = endX - registeredRect.X;
                            }
                        }
                    }

                    foreach(RectangleF delay in delayRectangles)
                    {
                        graphics.FillRectangle(e.Format.DelayBackFill, delay);
                    }

                    graphics.FillRectangle(e.Format.ForeFill, fill);

                    foreach (RectangleF delay in delayRectangles)
                    {
                        if(fill.IntersectsWith(delay))
                        {
                            float x = Math.Max(fill.X, delay.X);
                            float endX = Math.Min(fill.X + fill.Width, delay.X + delay.Width);
                            graphics.FillRectangle(e.Format.DelayForeFill, new RectangleF(x, fill.Y, endX - x, fill.Height));
                        }
                    }
                }
                else
                {
                    graphics.FillRectangle(e.Format.BackFill, taskRect);

                    // Draw delay
                    if (_mChartDelayRects.ContainsKey(task))
                    {
                        graphics.FillRectangle(e.Format.DelayBackFill, delayRect);
                    }

                    graphics.FillRectangle(e.Format.ForeFill, fill);
                    graphics.FillRectangle(e.Format.DelayForeFill, delayFill);
                    graphics.DrawRectangle(e.Format.Border, taskRect);
                }

                var rod = new RectangleF(taskRect.Left, taskRect.Top, taskRect.Width, taskRect.Height / 2);
                graphics.FillRectangle(Brushes.Black, rod);

                if (!task.IsCollapsed)
                {
                    // left bracket
                    graphics.FillPolygon(Brushes.Black, new PointF[] {
                                new PointF() { X = taskRect.Left, Y = taskRect.Top },
                                new PointF() { X = taskRect.Left, Y = taskRect.Top + BarHeight },
                                new PointF() { X = taskRect.Left + MinorWidth / 2f, Y = taskRect.Top } });
                    // right bracket
                    graphics.FillPolygon(Brushes.Black, new PointF[] {
                                new PointF() { X = taskRect.Right, Y = taskRect.Top },
                                new PointF() { X = taskRect.Right, Y = taskRect.Top + BarHeight },
                                new PointF() { X = taskRect.Right - MinorWidth / 2f, Y = taskRect.Top } });
                }
            }
            else
            {
                graphics.FillRectangle(e.Format.BackFill, taskRect);

                // Draw delay
                if (_mChartDelayRects.ContainsKey(task))
                {
                    graphics.FillRectangle(e.Format.DelayBackFill, delayRect);
                }

                graphics.FillRectangle(e.Format.ForeFill, fill);
                graphics.FillRectangle(e.Format.DelayForeFill, delayFill);
                graphics.DrawRectangle(e.Format.Border, taskRect);
            }
        }

        private void __DrawTaskParts(Graphics graphics, TaskPaintEventArgs e, Task task, Pen pen)
        {
            var parts = _mChartTaskPartRects[task];

            // Draw line indicator
            var firstRect = parts[0].Value;
            var lastRect = parts[parts.Count - 1].Value;
            var y_coord = (firstRect.Top + firstRect.Bottom) / 2.0f;
            var point1 = new PointF(firstRect.Right, y_coord);
            var point2 = new PointF(lastRect.Left, y_coord);
            graphics.DrawLine(pen, point1, point2);

            // Draw Part Rectangles
            var taskRects = parts.Select(x => x.Value).ToArray();
            graphics.FillRectangles(e.Format.BackFill, taskRects);

            // Draw % complete indicators
            graphics.FillRectangles(e.Format.ForeFill, parts.Select(x => new RectangleF(x.Value.X, x.Value.Y, x.Value.Width * x.Key.Complete, x.Value.Height)).ToArray());

            // Draw delay
            if (_mChartDelayRects.ContainsKey(task))
            {
                var delayRect = _mChartDelayRects[task];
                graphics.FillRectangle(e.Format.DelayBackFill, delayRect);

                // Calculated delayed complete rect
                var delayFill = _mChartDelayRects[task];
                var completionX = lastRect.X + lastRect.Width * parts[parts.Count - 1].Key.Complete;
                if (completionX > delayFill.Left)
                {
                    delayFill.Width = completionX - delayFill.Left;
                }
                else
                {
                    delayFill = Rectangle.Empty;
                }
                graphics.FillRectangle(e.Format.DelayForeFill, delayFill);
            }

            // Draw border
            graphics.DrawRectangles(e.Format.Border, taskRects);
        }

        private bool _isWithinHitBoxEdge(RectangleF rect, PointF point, int distance)
        {
            RectangleF innerExclusion = new RectangleF(rect.Left + distance, rect.Top, rect.Width - distance * 2, rect.Height);
            return !innerExclusion.Contains(point);
        }

        #endregion Private Helper Methods

        #region Private Helper Variables'
        /// <summary>
        /// Printing labels for header
        /// </summary>
        private static readonly SortedDictionary<DayOfWeek, string> ShortDays = new SortedDictionary<DayOfWeek, string>
        {
            {DayOfWeek.Sunday, "S"},
            {DayOfWeek.Monday, "M"},
            {DayOfWeek.Tuesday, "T"},
            {DayOfWeek.Wednesday, "W"},
            {DayOfWeek.Thursday, "T"},
            {DayOfWeek.Friday, "F"},
            {DayOfWeek.Saturday, "S"}
        };

        /// <summary>
        /// Polygon points for Header markers
        /// </summary>
        private static readonly PointF[] _Marker = new PointF[] {
            new PointF(-4, 0),
            new PointF(4, 0),
            new PointF(4, 4),
            new PointF(0, 8),
            new PointF(-4f, 4)
        };

        class HeaderInfo
        {
            public RectangleF H1Rect;
            public RectangleF H2Rect;
            public List<RectangleF> LabelRects;
            public List<RectangleF> Columns;
            public List<DateTime> DateTimes;
        }

        ProjectManager<Task, object> m_Project = null; // The project to be visualised / rendered as a Gantt Chart
        public ControlViewport Viewport = null;
        Task _mDraggedTask = null; // The dragged source Task
        Point _mDragTaskLastLocation = Point.Empty; // Record the task dragging mouse offset
        Point _mDragTaskStartLocation = Point.Empty;
        Point _mPanViewLastLocation = Point.Empty;
        Point _mPanViewStartLocation = Point.Empty;
        List<Task> _mSelectedTasks = new List<Task>(); // List of selected tasks
        Dictionary<Task, RectangleF> _mChartTaskHitRects = new Dictionary<Task, RectangleF>(); // list of hitareas for Task Rectangles
        Dictionary<Task, RectangleF> _mChartTaskRects = new Dictionary<Task, RectangleF>();
        Dictionary<Task, List<KeyValuePair<Task, RectangleF>>> _mChartTaskPartRects = new Dictionary<Task, List<KeyValuePair<Task, RectangleF>>>();
        Dictionary<Task, RectangleF> _mChartSlackRects = new Dictionary<Task, RectangleF>();
        Dictionary<Task, RectangleF> _mChartDelayRects = new Dictionary<Task, RectangleF>();
        HeaderInfo _mHeaderInfo = new HeaderInfo();
        Task _mBarMouseEntered = null; // flag whether the mouse has entered a hitbox of a task or not
        Dictionary<Task, string> _mTaskToolTip = new Dictionary<Task, string>();

        public bool SmartView = true;
        private bool _IsNearEdge;
        private bool _IsDragStart;
        #endregion Private Helper Variables
    }

    #region Chart Formatting

    /// <summary>
    /// Time resolution for the minor tick marks which are spaced Chart.TimeWidth apart
    /// </summary>
    public enum TimeResolution
    {
        Week,
        Day,
        Hour,
        Minute,
        Second,
    }

    /// <summary>
    /// Format for painting tasks
    /// </summary>
    public struct TaskFormat
    {
        /// <summary>
        /// Get or set Task outline color
        /// </summary>
        public Pen Border { get; set; }

        /// <summary>
        /// Get or set Task background color
        /// </summary>
        public Brush BackFill { get; set; }

        /// <summary>
        /// Get or set Task foreground color
        /// </summary>
        public Brush ForeFill { get; set; }

        /// <summary>
        /// Get or set Task delay background color
        /// </summary>
        public Brush DelayBackFill { get; set; }

        /// <summary>
        /// Get or set Task delay foreground color
        /// </summary>
        public Brush DelayForeFill { get; set; }

        /// <summary>
        /// Get or set Task font color
        /// </summary>
        public Brush Color { get; set; }

        /// <summary>
        /// Get or set the brush for slack bars
        /// </summary>
        public Brush SlackFill { get; set; }
    }

    /// <summary>
    /// Format for painting relations
    /// </summary>
    public struct RelationFormat
    {
        /// <summary>
        /// Get or set the line pen
        /// </summary>
        public Pen Line { get; set; }
    }

    /// <summary>
    /// Format for painting chart header
    /// </summary>
    public struct HeaderFormat
    {
        /// <summary>
        /// Font color
        /// </summary>
        public Brush Color { get; set; }
        /// <summary>
        /// Border and line colors
        /// </summary>
        public Pen Border { get; set; }
        /// <summary>
        /// Get or set the lighter color in the gradient
        /// </summary>
        public Color GradientLight { get; set; }
        /// <summary>
        /// Get or set the darker color in the gradient
        /// </summary>
        public Color GradientDark { get; set; }
    }

    public struct LabelFormat
    {
        public string Text;
        public Font Font;
        public Brush Color;
        public ChartTextAlign TextAlign;
        public float Margin;
    }
    #endregion Chart Formatting

    #region EventAgrs
    /// <summary>
    /// Provides data for TaskMouseEvent
    /// </summary>
    public class TaskMouseEventArgs : MouseEventArgs
    {
        /// <summary>
        /// Subject Task of the event
        /// </summary>
        public Task Task { get; private set; }
        /// <summary>
        /// Rectangle bounds of the Task
        /// </summary>
        public RectangleF Rectangle { get; private set; }
        /// <summary>
        /// Initialize a new instance of TaskMouseEventArgs with the MouseEventArgs parameters and the Task involved.
        /// </summary>
        public TaskMouseEventArgs(Task task, RectangleF rectangle, MouseButtons buttons, int clicks, int x, int y, int delta)
            : base(buttons, clicks, x, y, delta)
        {
            this.Task = task;
            this.Rectangle = rectangle;
        }
    }

    /// <summary>
    /// Provides data for TaskDragDropEvent
    /// </summary>
    public class MousePanEventArgs : MouseEventArgs
    {
        /// <summary>
        /// Get the previous mouse location
        /// </summary>
        public Point PreviousLocation { get; private set; }
        /// <summary>
        /// Get the starting mouse location of this drag drop event
        /// </summary>
        public Point StartLocation { get; private set; }
        /// <summary>
        /// Initialize a new instance of TaskDragDropEventArgs with the MouseEventArgs parameters and the Task involved and the previous mouse location.
        /// </summary>
        public MousePanEventArgs(Point startLocation, Point prevLocation, MouseButtons buttons, int clicks, int x, int y, int delta)
            : base(buttons, clicks, x, y, delta)
        {
            this.PreviousLocation = prevLocation;
            this.StartLocation = startLocation;
        }
    }

    /// <summary>
    /// Provides data for TaskDragDropEvent
    /// </summary>
    public class TaskDragDropEventArgs : MouseEventArgs
    {
        /// <summary>
        /// Get the previous mouse location
        /// </summary>
        public Point PreviousLocation { get; private set; }
        /// <summary>
        /// Get the starting mouse location of this drag drop event
        /// </summary>
        public Point StartLocation { get; private set; }
        /// <summary>
        /// Get the source task that is being dragged
        /// </summary>
        public Task Source { get; private set; }
        /// <summary>
        /// Get the target task that is being dropped on
        /// </summary>
        public Task Target { get; private set; }
        /// <summary>
        /// Get the rectangle bounds of the source task in chart coordinates
        /// </summary>
        public RectangleF SourceRect { get; private set; }
        /// <summary>
        /// Get the rectangle bounds of the target task in chart coordinates
        /// </summary>
        public RectangleF TargetRect { get; private set; }
        /// <summary>
        /// Get the chart row number that the mouse is current at.
        /// </summary>
        public int Row { get; private set; }
        /// <summary>
        /// Initialize a new instance of TaskDragDropEventArgs with the MouseEventArgs parameters and the Task involved and the previous mouse location.
        /// </summary>
        public TaskDragDropEventArgs(Point startLocation, Point prevLocation, Task source, RectangleF sourceRect, Task target, RectangleF targetRect, int row, MouseButtons buttons, int clicks, int x, int y, int delta)
            : base(buttons, clicks, x, y, delta)
        {
            this.Source = source;
            this.SourceRect = sourceRect;
            this.Target = target;
            this.TargetRect = targetRect;
            this.PreviousLocation = prevLocation;
            this.StartLocation = startLocation;
            this.Row = row;
        }
    }

    /// <summary>
    /// Provides data for ChartPaintEvent
    /// </summary>
    public class ChartPaintEventArgs : PaintEventArgs
    {
        /// <summary>
        /// Get the chart that for this event
        /// </summary>
        public GanttChart Chart { get; private set; }

        /// <summary>
        /// Initialize a new instance of ChartPaintEventArgs with the PaintEventArgs graphics and clip rectangle, and the chart itself.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="clipRect"></param>
        /// <param name="chart"></param>
        public ChartPaintEventArgs(Graphics graphics, Rectangle clipRect, GanttChart chart)
            : base(graphics, clipRect)
        {
            this.Chart = chart;
        }
    }

    /// <summary>
    /// Provides data for ChartPaintEvent
    /// </summary>
    public class HeaderPaintEventArgs : ChartPaintEventArgs
    {
        /// <summary>
        /// Get or set the font to use for drawing the text on the header
        /// </summary>
        public Font Font { get; set; }
        /// <summary>
        /// Get or set the header formatting
        /// </summary>
        public HeaderFormat Format { get; set; }

        /// <summary>
        /// Initialize a new instance of HeaderPaintEventArgs with the editable default font and header format
        /// </summary>
        public HeaderPaintEventArgs(Graphics graphics, Rectangle clipRect, GanttChart chart, Font font, HeaderFormat format)
            : base(graphics, clipRect, chart)
        {
            this.Font = font;
            this.Format = format;
        }
    }

    /// <summary>
    /// Provides data for TaskPaintEvent
    /// </summary>
    public class TaskPaintEventArgs : ChartPaintEventArgs
    {
        /// <summary>
        /// Get the task to be painted
        /// </summary>
        public Task Task { get; private set; }
        /// <summary>
        /// Get the row number of the task
        /// </summary>
        public int Row { get; private set; }
        /// <summary>
        /// Get or set the font to be used to draw the task label
        /// </summary>
        public Font Font { get; set; }
        /// <summary>
        /// Get or set the formatting of the task
        /// </summary>
        public TaskFormat Format { get; set; }
        /// <summary>
        /// Get whether the task is in a critical path
        /// </summary>
        public bool IsCritical { get; private set; }
        /// <summary>
        /// Initialize a new instance of TaskPaintEventArgs with the editable default font and task paint format
        /// </summary>
        public TaskPaintEventArgs(Graphics graphics, Rectangle clipRect, GanttChart chart, Task task, int row, bool critical, Font font, TaskFormat format) // need to create a paint event for each task for custom painting
            : base(graphics, clipRect, chart)
        {
            this.Task = task;
            this.Row = row;
            this.Font = font;
            this.Format = format;
            this.IsCritical = critical;
        }
    }

    /// <summary>
    /// Provides data for RelationPaintEvent
    /// </summary>
    public class RelationPaintEventArgs : ChartPaintEventArgs
    {
        /// <summary>
        /// Get the precedent task in the relation
        /// </summary>
        public Task Precedent { get; private set; }

        /// <summary>
        /// Get the dependant task in the relation
        /// </summary>
        public Task Dependant { get; private set; }

        /// <summary>
        /// Get or set the formatting to use for drawing the relation
        /// </summary>
        public RelationFormat Format { get; set; }

        /// <summary>
        /// Initialize a new instance of RelationPaintEventArgs with the editable default font and relation paint format
        /// </summary>
        public RelationPaintEventArgs(Graphics graphics, Rectangle clipRect, GanttChart chart, Task before, Task after, RelationFormat format)
            : base(graphics, clipRect, chart)
        {
            this.Precedent = before;
            this.Dependant = after;
            this.Format = format;
        }
    }

    /// <summary>
    /// Provides data for ScalePaintEvent
    /// </summary>
    public class TimelinePaintEventArgs : ChartPaintEventArgs
    {
        /// <summary>
        /// Get the datetime value of the tick mark
        /// </summary>
        public DateTime DateTime { get; private set; }
        /// <summary>
        /// Get the dateimte value of the preview mark
        /// </summary>
        public DateTime DateTimePrev { get; private set; }
        /// <summary>
        /// Get or set whether painting of the tick mark has already been handled. If it is already handled, Chart will not paint the tick mark.
        /// </summary>
        public bool Handled { get; private set; }
        /// <summary>
        /// Get or set the label for the minor scale
        /// </summary>
        LabelFormat Minor { get; set; }
        /// <summary>
        /// Get or set the label for the major scale
        /// </summary>
        LabelFormat Major { get; set; }

        public TimelinePaintEventArgs(Graphics graphics, Rectangle clipRect, GanttChart chart, DateTime datetime, DateTime datetimeprev, LabelFormat minor, LabelFormat major)
            : base(graphics, clipRect, chart)
        {
            Handled = false;
            DateTime = datetime;
            DateTimePrev = datetimeprev;
            Minor = minor;
            Major = major;
        }
    }

    #endregion EventArgs

    /// <summary>
    /// Provides information about the chart at a specific row and date/time.
    /// </summary>
    public struct ChartInfo
    {
        /// <summary>
        /// Get or set the chart row number
        /// </summary>
        public int Row { get; set; }
        /// <summary>
        /// Get or set the chart date/time
        /// </summary>
        public DateTime DateTime { get; set; }
        /// <summary>
        /// Get or set the task
        /// </summary>
        public Task Task { get; set; }
        /// <summary>
        /// Construct a passive data structure to hold chart information
        /// </summary>
        /// <param name="row"></param>
        /// <param name="dateTime"></param>
        /// <param name="task"></param>
        public ChartInfo(int row, DateTime dateTime, Task task)
            : this()
        {
            Row = row;
            DateTime = dateTime;
            Task = task;
        }
    }

    public enum ChartTextAlign
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }
}
