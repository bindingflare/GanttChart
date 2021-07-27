using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Edcore.GanttChart
{
    public partial class TaskListView : UserControl
    {
        private ProjectManager<Task, object> m_Project;
        private GanttChart m_Chart;
        IViewport m_ListViewport = null;
        TextBox m_DataTextBox = null;
        ListView m_TaskListView;

        Dictionary<Task, RectangleF> m_TaskRectangles;
        List<Task> _mSelectedTasks = new List<Task>(); // List of selected tasks
        Task _mListMouseEntered = null; // flag whether the mouse has entered a hitbox in the task list or not
        string _textBoxText = null;
        float _taskListWidth;
        float _taskListMinWidth;
        int[] _tasklistHeaderOrderList;
        bool isHitHeader;

        Dictionary<Task, List<RectangleF>> _mChartTaskListHitRects = new Dictionary<Task, List<RectangleF>>(); // list of hitareas for tasklist fields
        List<RectangleF> _mChartTaskHeaderHitRects = new List<RectangleF>(); // list of hitareas for tasklist headers

        public TaskListView()
        {
            // Designer values
            InitializeComponent();

            // Date text box
            m_DataTextBox = new TextBox();
            m_DataTextBox.BorderStyle = BorderStyle.Fixed3D;
            m_DataTextBox.Font = Font;
            m_DataTextBox.TextChanged += new EventHandler(dataTextBox_TextChanged);
            m_DataTextBox.Visible = false;
            this.Controls.Add(m_DataTextBox);

            // Factory Values
            _taskListWidth = 500;
            _taskListMinWidth = 200;
            int barSpacing = 32;
            m_ListViewport = new ControlViewport(this) { WheelDelta = barSpacing };

            // Formatting
            TaskFormat = new Edcore.GanttChart.TaskFormat()
            {
                Color = Brushes.Black,
                Border = Pens.Maroon,
                BackFill = Brushes.MediumSlateBlue,
                ForeFill = Brushes.YellowGreen,
                SlackFill = new System.Drawing.Drawing2D.HatchBrush(System.Drawing.Drawing2D.HatchStyle.LightDownwardDiagonal, Color.Blue, Color.Transparent)
            };
            CriticalTaskFormat = new Edcore.GanttChart.TaskFormat()
            {
                Color = Brushes.Black,
                Border = Pens.Maroon,
                BackFill = Brushes.Crimson,
                ForeFill = Brushes.YellowGreen,
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

        #region Public Methods

        public void Init(ProjectManager<Task, object> project, GanttChart chart)
        {
            m_Project = project;
            m_Chart = chart;

            // Listen to scroll of Chart
            m_Chart.Scroll += GanttChart_Scroll;
            m_Chart.MouseWheel += GanttChart_Mousewheel;

            
            SetTaskRects(m_Chart.GetTaskRectangles());
            RecalculateTasklist();

            // Listview
            m_TaskListView = new ListView();
            m_TaskListView.Location = new Point(0, m_Chart.HeaderOneHeight);
            m_TaskListView.Size = new Size((int)_taskListWidth, (int)(m_ListViewport.Rectangle.Height));

            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(1, 30);
            m_TaskListView.SmallImageList = imgList;

            m_TaskListView.View = View.Details;
            this.Controls.Add(m_TaskListView);
        }

        /// <summary>
        /// Occurs when a Task in TaskList is clicked
        /// </summary>
        public event EventHandler<TaskListMouseEventArgs> TaskListMouseClick = null;

        public void RecalculateTasklist()
        {
            _tasklistHeaderOrderList = new int[m_Project.FieldCount];
            float totalSize = 0;

            for (int index = 0; index < m_Project.FieldCount; index++)
            {
                if (!m_Project.GetFieldHidden(index))
                    totalSize += m_Project.GetFieldSize(index);

                _tasklistHeaderOrderList[m_Project.GetFieldPriority(index)] = index;
            }

            if (totalSize < _taskListMinWidth)
                _taskListWidth = _taskListMinWidth;
            else
                _taskListWidth = totalSize;

            m_ListViewport.WorldHeight = this.Height;
            m_ListViewport.WorldWidth = _taskListWidth;
        }

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

        public void SetTaskRects(Dictionary<Task, RectangleF> rects)
        {
            m_TaskRectangles = rects;
        }

        public int SelectedFieldIndex { get; private set; }

        #endregion Public Methods

        #region EventArgs

        /// <summary>
        /// Raises the System.Windows.Forms.Control.Paint event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            if (!this.DesignMode)
            {
                _DrawTitle(e.Graphics, e.ClipRectangle);
                _DrawTaskList(e.Graphics, e.ClipRectangle);
            }
        }
        private void GanttChart_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
                this.Invalidate();
        }
        private void GanttChart_Mousewheel(object sender, MouseEventArgs e)
        {
            this.Invalidate();
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseMove event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            var deviceLocation = m_ListViewport.DeviceToWorldCoord(e.Location);

            // Hot tracking for list
            var chartcoord = m_ListViewport.DeviceToWorldCoord(e.Location);

            bool _isHit = false;
            foreach (var hitTask in _mChartTaskListHitRects.Keys)
            {
                foreach (var hitBox in _mChartTaskListHitRects[hitTask])
                {
                    if (hitBox.Contains(chartcoord))
                    {
                        OnTaskListMouseOver(new TaskMouseEventArgs(hitTask, hitBox, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                        _isHit = true;
                        _mListMouseEntered = hitTask;
                    }
                }
            }

            foreach (var hitBox in _mChartTaskHeaderHitRects)
            {
                if (hitBox.Contains(chartcoord))
                {
                    this.Cursor = Cursors.Hand;
                    _isHit = true;
                    isHitHeader = true;
                }
            }

            if (!_isHit && (_mListMouseEntered != null || isHitHeader))
            {
                //OnTaskMouseOut(new TaskMouseEventArgs(_mListMouseEntered, RectangleF.Empty, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                _mListMouseEntered = null;
                isHitHeader = false;
            }

            // If mouse moved quickly from selecting a header, this will prevent indefinite hand (until some other event resets the mouse)
            if (isHitHeader)
            {
                //OnTaskMouseOut(new TaskMouseEventArgs(_mBarMouseEntered, RectangleF.Empty, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                isHitHeader = false;
            }


            // Panning mode
            //if (_mDraggedTask == null && e.Button == MouseButtons.Middle)
            //{
            //    this.Cursor = Cursors.SizeAll;
            //    m_ListViewport.X -= e.X - _mPanViewLastLocation.X;
            //    m_ListViewport.Y -= e.Y - _mPanViewLastLocation.Y;
            //    _mPanViewLastLocation = e.Location;
            //}
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseClick event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            var deviceLocation = m_ListViewport.DeviceToWorldCoord(e.Location);

            // Check if task list elements clicked
            bool isHit = false;

            foreach (var hitTask in _mChartTaskListHitRects.Keys.ToList())
            {
                var hitBoxes = _mChartTaskListHitRects[hitTask];

                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    RectangleF hitBox = hitBoxes.ElementAt(i);

                    if (hitBox.Contains(deviceLocation))
                    {
                        OnTaskListMouseClick(new TaskListMouseEventArgs(i, hitTask, hitBox, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                        isHit = true;
                        break;
                    }
                }
            }

            for (int j = 0; j < _mChartTaskHeaderHitRects.Count; j++)
            {
                var hitBox = _mChartTaskHeaderHitRects[j];

                if (hitBox.Contains(deviceLocation))
                {
                    OnTaskListMouseClick(new TaskListMouseEventArgs(j, null, hitBox, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                }
            }


            if (!isHit)
            {
                //OnTaskDeselecting(new TaskMouseEventArgs(task, RectangleF.Empty, e.Button, e.Clicks, e.X, e.Y, e.Delta));
            }
        }

        /// <summary>
        /// Raises the TaskListMouseOver event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskListMouseOver(TaskMouseEventArgs e)
        {
            TaskListMouseOver?.Invoke(this, e);

            this.Cursor = Cursors.Hand;

            var task = e.Task;
            if (m_Project.IsPart(e.Task)) task = m_Project.SplitTaskOf(task);
            //if (_mTaskToolTip.ContainsKey(task))
            //{
            //    _mOverlay.ShowToolTip(m_ListViewport.DeviceToWorldCoord(e.Location), _mTaskToolTip[task]);
            //    this.Invalidate();
            //}
        }

        /// <summary>
        /// Raises the TaskListMouseClick event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskListMouseClick(TaskListMouseEventArgs e)
        {
            TaskListMouseClick?.Invoke(this, e);

            if (e.Task != null)
            {
                if (e.Button == MouseButtons.Left)
                {
                    OnTaskDeselecting(e);
                    _mSelectedTasks.Add(e.Task);
                    //OnTaskSelected(e);

                    // Hover over edit 
                    RectangleF rect = e.Rectangle;
                    float width = rect.Width;
                    float height = rect.Height;
                    float left = rect.Left;
                    float top = rect.Top;

                    m_DataTextBox.Text = m_Project.GetData(e.Task, m_Project.GetFieldIndex(e.FieldOrder));
                    m_DataTextBox.Size = new Size((int)width, (int)height);
                    m_DataTextBox.Location = new Point((int)left, (int)top);
                    m_DataTextBox.Focus();
                    m_DataTextBox.Show();

                    SelectedFieldIndex = m_Project.GetFieldIndex(e.FieldOrder);

                }
                else if (e.Button == MouseButtons.Right)
                {
                    OnTaskDeselecting(e);
                    _mSelectedTasks.Add(e.Task);

                    SelectedFieldIndex = m_Project.GetFieldIndex(e.FieldOrder);
                    //OnTaskSelected(e);
                }
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    OnTaskDeselecting(e);
                    SelectedFieldIndex = m_Project.GetFieldIndex(e.FieldOrder);
                }
            }

        }

        /// <summary>
        /// Raises the TaskDeselecting event and then clear all the selected tasks
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskDeselecting(TaskMouseEventArgs e)
        {
            TaskDeselecting?.Invoke(this, e);

            // make textbox invisible
            if (m_DataTextBox.Visible)
            {
                if (SelectedFieldIndex <= 3)
                {
                    if (SelectedFieldIndex == 0)
                    {
                        SelectedTask.Name = _textBoxText;
                    }
                }
                else
                {
                    m_Project.SetCustomField(SelectedTask, SelectedFieldIndex - 4, _textBoxText);
                }
                m_DataTextBox.Visible = false;
            }



            // deselect all tasks
            _mSelectedTasks.Clear();
        }

        protected virtual void dataTextBox_TextChanged(object sender, EventArgs e)
        {
            _textBoxText = m_DataTextBox.Text;
        }

        /// <summary>
        /// Raises the System.Windows.Forms.Control.MouseDoubleClick event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var deviceLocation = m_ListViewport.DeviceToWorldCoord(e.Location);

            foreach (var hitTask in _mChartTaskListHitRects.Keys)
            {
                var hitBoxes = _mChartTaskListHitRects[hitTask];

                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    RectangleF hitBox = hitBoxes.ElementAt(i);

                    if (hitBox.Contains(deviceLocation))
                    {
                        OnTaskMouseDoubleClick(new TaskMouseEventArgs(hitTask, hitBox, e.Button, e.Clicks, e.X, e.Y, e.Delta));
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Raises the TaskMouseDoubleClick event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTaskMouseDoubleClick(TaskMouseEventArgs e)
        {
            //TaskMouseDoubleClick?.Invoke(this, e); // Handled by GanttChart

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                e.Task.IsCollapsed = !e.Task.IsCollapsed;
            }

            this.Invalidate();
        }

        /// <summary>
        /// Occurs before one or more tasks are being deselected. All Task in Chart.SelectedTasks will be deselected.
        /// </summary>
        public event EventHandler<TaskMouseEventArgs> TaskDeselecting = null;

        /// <summary>
        /// Occurs when a mouse hovers over the tasklist hitboxes.
        /// </summary>
        private event EventHandler<TaskMouseEventArgs> TaskListMouseOver = null;

        #endregion EventArgs

        #region Private Helper Methods

        private void _DrawTitle(Graphics graphics, Rectangle ClipRect)
        {
            // Draw title
            var titleRect = new RectangleF(0, m_ListViewport.Y, _taskListWidth, m_Chart.HeaderOneHeight);
            var gradient = new LinearGradientBrush(titleRect, HeaderFormat.GradientLight, HeaderFormat.GradientDark, 90f);

            graphics.FillRectangle(gradient, titleRect);
            graphics.DrawRectangle(HeaderFormat.Border, titleRect);

            var titleFont = new Font(Font.FontFamily, 18, FontStyle.Bold);
            var titleTextRect = graphics.TextBoxAlign(m_Project.Name, ChartTextAlign.MiddleLeft, titleFont, titleRect);
            graphics.DrawString(m_Project.Name, titleFont, Brushes.Black, titleTextRect);
        }
        private void _DrawTaskList(Graphics graphics, Rectangle ClipRect)
        {
            // Set model view matrix
            graphics.Transform = m_ListViewport.Projection;

            // Clear task list rectangles
            _mChartTaskListHitRects.Clear();
            _mChartTaskHeaderHitRects.Clear();
            m_TaskListView.Columns.Clear();
            m_TaskListView.Items.Clear();

            var pen = new Pen(Color.LightGray);
            var hBrush = Brushes.WhiteSmoke;
            var strBrush = Brushes.Gray;
            var colBrush = Brushes.White;
            float labelMargin = m_Chart.MinorWidth / 2.0f - 2f;

            // Convert Y
            float listTop = m_Chart.Viewport.Y;
            float headerStartY = m_ListViewport.Y + m_Chart.HeaderOneHeight;
            float listStartY = headerStartY + m_Chart.HeaderOneHeight;
            float listBottom = listTop + m_ListViewport.Rectangle.Bottom;

            // Draw headers and dividers
            float headerStart = 0;
            for (int order = 0; order < _tasklistHeaderOrderList.Count(); order++)
            {
                int index = _tasklistHeaderOrderList[order];
                string headerName = m_Project.GetFieldName(index);
                float size = m_Project.GetFieldSize(index);
                RectangleF headerRect = RectangleF.Empty;

                if (m_Project.GetFieldHidden(index) == false)
                {
                    headerRect = new RectangleF(headerStart, headerStartY, size, m_Chart.HeaderTwoHeight);

                    ColumnHeader header = new ColumnHeader();
                    header.Text = headerName;
                    header.TextAlign = HorizontalAlignment.Left;
                    header.Width = (int)size;
                    m_TaskListView.Columns.Add(header);

                    headerStart += size;
                }

                _mChartTaskHeaderHitRects.Add(headerRect);
            }

            // Draw task list
            foreach (var task in m_TaskRectangles.Keys)
            {
                float colStart = 0;

                var taskRect = m_TaskRectangles[task];
                

                if (taskRect.Bottom < listTop || taskRect.Top > listBottom) continue; // skip task that is not within bounds of task list rectangle

                // Convert Y
                float taskStartY = taskRect.Y - listTop;
                float taskEndY = taskStartY + taskRect.Height + 5;
                
                string[] items = new string[_tasklistHeaderOrderList.Count()];

                int j = 0;
                for (int i = 0; i < _tasklistHeaderOrderList.Count(); i++)
                {
                    int index = _tasklistHeaderOrderList[i];
                    string type = m_Project.GetFieldType(index);

                    if (m_Project.GetFieldHidden(index) == false)
                    {
                        string data = _ParseFieldData(index, task);
                        float size = m_Project.GetFieldSize(index);
                        var rect = graphics.TextBoxAlign(data, ChartTextAlign.MiddleLeft, Font, new RectangleF(colStart, taskStartY, size, Font.Height), labelMargin);
                        rect.Width = size - labelMargin * 2;

                        if (!_mChartTaskListHitRects.ContainsKey(task))
                            _mChartTaskListHitRects.Add(task, new List<RectangleF>());

                        _mChartTaskListHitRects[task].Add(rect);

                        items[j] = data;
                        j++;

                    }
                }

                ListViewItem item = new ListViewItem(items);
                m_TaskListView.Items.Add(item);
            }

            // Flush
            graphics.Flush();
        }

        private string _ParseFieldData(int index, Task task)
        {
            string type = m_Project.GetFieldType(index);
            if (type.Equals("string"))
            {
                return m_Project.GetCustomField(task, index);
            }
            else if (type.Equals("date"))
            {
                if (index == 1)
                    return m_Project.GetDateTime(task.Start).ToString("yyyy.MM.dd hh:mm:ss");
                if (index == 2)
                    return m_Project.GetDateTime(task.End).ToString("yyyy.MM.dd hh:mm:ss");
            }
            else if (type.Equals("time"))
            {
                if (index == 3)
                    return task.Duration.ToString(@"dd\.hh\:mm\:ss");
            }
            else
            {
                if (index == 0)
                    return task.Name;
            }

            return null;
        }


        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TaskListView
            // 
            this.Name = "TaskListView";
            this.Size = new System.Drawing.Size(343, 580);
            this.ResumeLayout(false);

        }

        #endregion Private Helper Methods

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
    }

    /// <summary>
    /// Provides data for TaskListMouseEvent
    /// </summary>
    public class TaskListMouseEventArgs : TaskMouseEventArgs
    {
        /// <summary>
        /// Subject Task of the event
        /// </summary>
        public int FieldOrder { get; private set; }

        /// Initialize a new instance of TaskMouseEventArgs with the MouseEventArgs parameters and the Task involved.
        /// </summary>
        public TaskListMouseEventArgs(int fieldOrder, Task task, RectangleF rectangle, MouseButtons buttons, int clicks, int x, int y, int delta)
            : base(task, rectangle, buttons, clicks, x, y, delta)
        {
            this.FieldOrder = fieldOrder;
        }
    }
}
