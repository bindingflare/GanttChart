using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Edcore.GanttChart
{
    /// <summary>
    /// An elaborate example on how the chart control might be used. 
    /// Start by collapsing all regions and then read the constructor.
    /// Refer to IProjectManager interface for method description.
    /// </summary>
    public partial class NewUI : Form
    {
        OverlayPainter m_Overlay = new OverlayPainter();
        ProjectManager m_Manager = null;
        Form taskForm = null;

        private int _splitterX;
        private int _scrollFix = 0;
        private bool _isPanning = false;
        private bool _searchMode = false;
        private Task m_SelectedTask;
        private OLVColumn m_SelectedTaskColumn;

        /// <summary>
        /// Example starts here
        /// </summary>
        public NewUI()
        {
            InitializeComponent();

            // Create a Project
            m_Manager = new ProjectManager("256K DRAM 7월 진척현황");

            // Title label
            projectTitleLabel.Text = m_Manager.Name;
            projectTitleLabel.Font = new Font(Font.FontFamily, 16f, FontStyle.Bold);

            // Initialize the Chart with our ProjectManager and CreateTaskDelegate
            m_Chart.Init(m_Manager);
            m_Chart.CreateTaskDelegate = delegate () { return new MyTask(m_Manager); };

            // Initialize the Tasklist
            m_TaskList.RowHeight = m_Chart.BarSpacing - 1;
            m_TaskList.CanExpandGetter = delegate (object x) { return ((Task)x).CanExpand; };
            m_TaskList.ChildrenGetter = delegate (object x) { return ((Task)x).Children; };
            m_TaskList.AllowColumnReorder = true;
            m_TaskList.CellEditActivation = ObjectListView.CellEditActivateMode.SingleClick;
            m_TaskList.CellEditUseWholeCell = true;

            // Set width of Tasklist using splitter
            m_SplitContainer.SplitterDistance = 400;

            _GenerateListView(m_TaskList, m_Manager.HeaderList);

            // Perform some task operations
            _registerCustomField("Important", "string", 80.0f);
            _registerCustomField("Cancelled", "string", 80.0f);

            var work = new MyTask(m_Manager) { Name = "Prepare for Work" };
            var wake = new MyTask(m_Manager) { Name = "Wake Up" };
            var teeth = new MyTask(m_Manager) { Name = "Brush Teeth" };
            var shower = new MyTask(m_Manager) { Name = "Shower" };
            var clothes = new MyTask(m_Manager) { Name = "Change into New Clothes" };
            var hair = new MyTask(m_Manager) { Name = "Blow My Hair" };
            var pack = new MyTask(m_Manager) { Name = "Pack the Suitcase" };

            m_Manager.Add(work);
            m_Manager.Add(wake);
            m_Manager.Add(teeth);
            m_Manager.Add(shower);
            m_Manager.Add(clothes);
            m_Manager.Add(hair);
            m_Manager.Add(pack);

            m_Manager.SetField(work, 7, "Hello");
            m_Manager.SetField(wake, 7, "HI");
            m_Manager.SetField(teeth, 7, "hey");
            m_Manager.SetField(shower, 7, "Yo");
            m_Manager.SetField(clothes, 7, "Greetings");
            m_Manager.SetField(hair, 7, "Good morning");
            m_Manager.SetField(pack, 7, "Good evening");

            var job1 = new MyTask(m_Manager) { Name = "Job1" };
            var job2 = new MyTask(m_Manager) { Name = "Job2" };
            var job3 = new MyTask(m_Manager) { Name = "Brush Teeth" };
            var job4 = new MyTask(m_Manager) { Name = "JOb4" };
            var job5 = new MyTask(m_Manager) { Name = "JOb 5" };
            var job6 = new MyTask(m_Manager) { Name = "job 6" };
            var job7 = new MyTask(m_Manager) { Name = "end job" };

            m_Manager.Add(job1);
            m_Manager.Add(job2);
            m_Manager.Add(job3);
            m_Manager.Add(job4);
            m_Manager.Add(job5);
            m_Manager.Add(job6);
            m_Manager.Add(job7);

            m_Manager.SetField(job1, 7, "Hello");
            m_Manager.SetField(job2, 7, "HI");
            m_Manager.SetField(job3, 7, "hey");
            m_Manager.SetField(job4, 7, "Yo");
            m_Manager.SetField(job5, 7, "Greetings");
            m_Manager.SetField(job6, 7, "Good morning");
            m_Manager.SetField(job7, 7, "Good evening");

            //m_Manager.SetField(work, "Important", "Yes");

            // Test delay
            m_Manager.SetDelay(job2, new TimeSpan(3, 0, 0, 0));
            m_Manager.SetDelay(job3, new TimeSpan(1, 0, 0, 0));
            m_Manager.SetDelay(job4, new TimeSpan(7, 0, 0, 0));
            m_Manager.SetDelay(job7, new TimeSpan(3, 0, 0, 0));

            // Create another 1000 tasks for stress testing
            Random rand = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var task = new MyTask(m_Manager) { Name = string.Format("New Task {0}", i.ToString()) };
                m_Manager.Add(task);
                m_Manager.SetStart(task, TimeSpan.FromDays(rand.Next(100)));
                m_Manager.SetDuration(task, TimeSpan.FromDays(rand.Next(50)));
            }

            // Set task durations, e.g. using ProjectManager methods 
            m_Manager.SetDuration(wake, TimeSpan.FromDays(3));
            m_Manager.SetDuration(teeth, TimeSpan.FromDays(5));
            m_Manager.SetDuration(shower, TimeSpan.FromDays(7));
            m_Manager.SetDuration(clothes, TimeSpan.FromDays(4));
            m_Manager.SetDuration(hair, TimeSpan.FromDays(3));
            m_Manager.SetDuration(pack, TimeSpan.FromDays(5));
            m_Manager.SetDuration(job2, TimeSpan.FromDays(3));
            m_Manager.SetDuration(job3, TimeSpan.FromDays(5));
            m_Manager.SetDuration(job4, TimeSpan.FromDays(7));
            m_Manager.SetDuration(job5, TimeSpan.FromDays(4));
            m_Manager.SetDuration(job6, TimeSpan.FromDays(3));
            m_Manager.SetDuration(job7, TimeSpan.FromDays(5));

            // demostrate splitting a task
            m_Manager.Split(pack, new MyTask(m_Manager), new MyTask(m_Manager), TimeSpan.FromDays(2));
            m_Manager.Split(job7, new MyTask(m_Manager), new MyTask(m_Manager), TimeSpan.FromDays(2));

            // Set task complete status, e.g. using newly created properties
            wake.Complete = 0.9f;
            teeth.Complete = 0.5f;
            shower.Complete = 0.4f;

            job2.Complete = 0.9f;
            job3.Complete = 0.5f;
            job4.Complete = 0.4f;

            // Give the Tasks some organisation, setting group and precedents
            m_Manager.Group(work, wake);
            m_Manager.Group(work, teeth);
            m_Manager.Group(work, shower);
            m_Manager.Group(work, clothes);
            m_Manager.Group(work, hair);
            m_Manager.Group(work, pack);
            m_Manager.Relate(wake, teeth);
            m_Manager.Relate(wake, shower);
            m_Manager.Relate(shower, clothes);
            m_Manager.Relate(shower, hair);
            m_Manager.Relate(hair, pack);
            m_Manager.Relate(clothes, pack);

            m_Manager.Group(job1, job2);
            m_Manager.Group(job1, job3);
            m_Manager.Group(job1, job4);
            m_Manager.Group(job1, job5);
            //m_Manager.Group(job1, job6);
            m_Manager.Group(job1, job7);
            m_Manager.Relate(job2, job3);
            m_Manager.Relate(job2, job4);
            m_Manager.Relate(job4, job5);
            //m_Manager.Relate(job4, job6);
            //m_Manager.Relate(job6, job7);
            m_Manager.Relate(job5, job7);

            // Create and assign Resources.
            // MyResource is just custom user class. The API can accept any object as resource.
            var jake = new MyResource() { Name = "Jake" };
            var peter = new MyResource() { Name = "Peter" };
            var john = new MyResource() { Name = "John" };
            var lucas = new MyResource() { Name = "Lucas" };
            var james = new MyResource() { Name = "James" };
            var mary = new MyResource() { Name = "Mary" };

            // Add some resources
            m_Manager.Assign(job2, jake);
            m_Manager.Assign(job2, peter);
            m_Manager.Assign(job2, john);
            m_Manager.Assign(job3, jake);
            m_Manager.Assign(job3, james);
            m_Manager.Assign(job7, james);
            m_Manager.Assign(job7, lucas);
            m_Manager.Assign(job4, mary);
            m_Manager.Assign(job4, lucas);
            m_Manager.Assign(job4, john);

            // Build tasklist
            IEnumerable<Task> tasks = m_Manager.RootTasks;
            foreach (Task task in tasks)
            {
                task.Children = m_Manager.DirectMembersOf(task);
                if (task.Children.Count() != 0)
                {
                    task.CanExpand = true;
                }
            }
            m_TaskList.Roots = tasks;
            m_TaskList.ExpandAll();
            m_TaskList.Refresh();

            // GanttChart event listeners
            m_Chart.TaskMouseOver += new EventHandler<TaskMouseEventArgs>(m_Chart_TaskMouseOver);
            m_Chart.TaskMouseOut += new EventHandler<TaskMouseEventArgs>(m_Chart_TaskMouseOut);
            m_Chart.TaskSelected += new EventHandler<TaskMouseEventArgs>(m_Chart_TaskSelected);
            m_Chart.TaskDeselecting += new EventHandler<TaskMouseEventArgs>(m_Chart_TaskDeselecting);
            m_Chart.TaskMouseDoubleClick += new EventHandler<TaskMouseEventArgs>(m_Chart_TaskMouseDoubleClick);
            m_Overlay.PrintMode = true;
            m_Chart.PaintOverlay += m_Overlay.ChartOverlayPainter;
            m_Chart.AllowTaskDragDrop = true;
            //m_Chart.Scroll += new ScrollEventHandler(m_Chart_Scroll);
            m_Chart.MouseWheel += new MouseEventHandler(m_Chart_MouseWheel);
            m_Chart.MousePan += new EventHandler<MousePanEventArgs>(m_Chart_MousePan);

            // Tasklist event listeners
            m_TaskList.Expanded += new EventHandler<TreeBranchExpandedEventArgs>(m_TaskList_Expanded);
            m_TaskList.Collapsed += new EventHandler<TreeBranchCollapsedEventArgs>(m_TaskList_Collapsed);
            m_TaskList.CellEditValidating += new CellEditEventHandler(m_TaskList_CellEditValidating);
            m_TaskList.CellEditFinishing += new CellEditEventHandler(m_TaskList_CellEditFinishing);
            m_TaskList.Scroll += new EventHandler<ScrollEventArgs>(m_TaskList_Scroll);
            m_TaskList.Expanding += new EventHandler<TreeBranchExpandingEventArgs>(m_TaskList_Expanding);
            m_TaskList.Collapsing += new EventHandler<TreeBranchCollapsingEventArgs>(m_TaskList_Collapsing);
            m_TaskList.ColumnClick += new ColumnClickEventHandler(m_Tasklist_ColumnRightClick);
            m_TaskList.CellRightClick += new EventHandler<CellRightClickEventArgs>(m_TaskList_CellRightClick);
            m_TaskList.CellEditStarting += (sender, args) =>
            {
                // Left align the edit control
                args.Control.Location = args.CellBounds.Location;

                // Readjust the size of the control to fill the whole cell if CellEditUseWholeCellEffective is enabled
                if (args.Column.CellEditUseWholeCellEffective)
                {
                    args.Control.Size = args.CellBounds.Size;
                }

            };

            // Splitter event listeners
            //m_SplitContainer.SplitterMoving += new SplitterCancelEventHandler(m_SplitContainer_SplitterMoving);
            //m_SplitContainer.SplitterMoved += new SplitterEventHandler(m_SplitContainer_SplitterMoved);
            m_SplitContainer.MouseDown += new MouseEventHandler(m_SplitContainer_MouseDown);
            m_SplitContainer.MouseUp += new MouseEventHandler(m_SplitContainer_MouseUp);
            m_SplitContainer.MouseMove += new MouseEventHandler(m_SplitContainer_MouseMove);

            // Set some tooltips to show the resources in each task
            //_mChart.SetToolTip(wake, string.Join(", ", _mManager.ResourcesOf(wake).Select(x => (x as MyResource).Name)));
            //_mChart.SetToolTip(teeth, string.Join(", ", _mManager.ResourcesOf(teeth).Select(x => (x as MyResource).Name)));
            //_mChart.SetToolTip(pack, string.Join(", ", _mManager.ResourcesOf(pack).Select(x => (x as MyResource).Name)));
            //_mChart.SetToolTip(shower, string.Join(", ", _mManager.ResourcesOf(shower).Select(x => (x as MyResource).Name)));

            // Set Time information
            var span = DateTime.Today - m_Manager.Start;
            m_Manager.Now = span; // set the "Now" marker at the correct date
            m_Chart.TimeResolution = TimeResolution.Day; // Set the chart to display in days in header

            // Enable second form
            taskForm = new Form();
            taskForm.TopMost = true;
            taskForm.Text = "Tasks";
            taskForm.Controls.Add(taskTabControl);
            taskForm.FormClosing += TaskForm_FormClosing;

            taskTabControl.Dock = DockStyle.Fill;
            taskTabControl.Visible = true;

            // Init the rest of the UI
            _InitExampleUI();
        }

        

        private void m_Chart_MousePan(object sender, MousePanEventArgs e)
        {
            // Prevent tasklist's Scroll event from mixing with panning functionality
            _isPanning = true;
            m_TaskList.LowLevelScroll(0, e.StartLocation.Y - e.PreviousLocation.Y);
            _isPanning = false;
        }

        private void m_Tasklist_ColumnRightClick(object sender, ColumnClickEventArgs e)
        {
            ToolStripDropDown toolstrip = new ToolStripDropDown();
            m_TaskList.MakeColumnCommandMenu(toolstrip, e.Column);

            int x = 0;
            int fullWidth = 0;
            foreach (OLVColumn column in m_TaskList.Columns)
            {
                if (column.Index == e.Column)
                {
                    x = fullWidth;
                }

                fullWidth += column.Width;
            }

            x = x - m_TaskList.LowLevelScrollPosition.X;
            toolstrip.Items[2].Click += NewUI_Click; ;
            toolstrip.Show(m_TaskList, x, m_Chart.HeaderTwoHeight);
        }

        private void NewUI_Click(object sender, EventArgs e)
        {
            m_TaskList.Roots = m_Manager.RootTasks;
        }

        private void m_TaskList_CellRightClick(object sender, CellRightClickEventArgs e)
        {
            taskMenuStrip.Show(m_TaskList, e.Location);
            m_SelectedTask = (Task)e.Model;
            m_SelectedTaskColumn = e.Column;
        }

        private void m_TaskList_Filter(object sender, FilterEventArgs e)
        {
            IEnumerable<Task> tasks = (IEnumerable<Task>)e.Objects;

            foreach (object obj in e.Objects)
            {
                Task task = (Task)obj;

                if (tasks.Contains(task))
                {
                    task.IsFiltered = false;
                }
                else
                {
                    task.IsFiltered = true;
                }
            }
        }

        private void m_TaskList_Collapsing(object sender, TreeBranchCollapsingEventArgs e)
        {
            if (_searchMode)
            {
                e.Canceled = true;
            }
        }

        private void m_TaskList_Expanding(object sender, TreeBranchExpandingEventArgs e)
        {
            if (_searchMode)
            {
                e.Canceled = true;
            }
        }

        private void SearchTextBox_LostFocus(object sender, EventArgs e)
        {
            if (m_SearchTextBox.Text.Length == 0)
            {
                _searchMode = false;
                m_SearchTextBox.Text = "Search...";
            }
        }

        private void SearchTextBox_GotFocus(object sender, EventArgs e)
        {
            if (m_SearchTextBox.Text == "Search...")
            {
                _searchMode = true;
                m_SearchTextBox.Text = "";
            }

        }

        private void m_SplitContainer_SplitterMoving(object sender, SplitterCancelEventArgs e)
        {
            m_SplitContainer.SuspendLayout();
        }

        private void m_SplitContainer_SplitterMoved(object sender, SplitterEventArgs e)
        {
            float maxSize = 0;
            foreach (OLVColumn column in m_TaskList.AllColumns)
            {
                if (column.IsVisible)
                {
                    maxSize += column.Width;
                }
            }

            if (e.X > maxSize)
            {
                m_SplitContainer.SplitterDistance = (int)maxSize + 24;
            }
        }

        private void m_SplitContainer_MouseDown(object sender, MouseEventArgs e)
        {
            ((SplitContainer)sender).IsSplitterFixed = true;

            // Calculate max size
            float maxSize = 0;
            foreach (OLVColumn column in m_TaskList.AllColumns)
            {
                if (column.IsVisible)
                {
                    maxSize += column.Width;
                }
            }

            _splitterX = (int) maxSize + 24;
        }
        
        private void m_SplitContainer_MouseUp(object sender, MouseEventArgs e)
        {
            ((SplitContainer)sender).IsSplitterFixed = false;
        }
        
        private void m_SplitContainer_MouseMove(object sender, MouseEventArgs e)
        {
            // Check to make sure the splitter won't be updated by the
            // normal move behavior also
            if (((SplitContainer)sender).IsSplitterFixed)
            {
                if (e.Button.Equals(MouseButtons.Left))
                {
                    if (((SplitContainer)sender).Orientation.Equals(Orientation.Vertical))
                    {
                        // Update splitter distance and refresh
                        int newSize = e.X;

                        if (newSize > 0)
                        {
                            if(newSize > _splitterX)
                            {
                                newSize = _splitterX;
                            }
                            
                            ((SplitContainer)sender).SplitterDistance = newSize;
                            ((SplitContainer)sender).Refresh();
                        }
                    }
                }
                else
                {
                    ((SplitContainer)sender).IsSplitterFixed = false;
                }
            }
        }

        private void m_TaskList_CellEditFinishing(object sender, CellEditEventArgs e)
        {
            m_Chart.Invalidate();
        }

        private void m_TaskList_CellEditValidating(object sender, CellEditEventArgs e)
        {
            //int index = e.ListViewItem.Index;

            //if (e.NewValue is string)
            //{
            //    if (!m_Manager.SetField((Task)e.RowObject, index, (string)e.NewValue))
            //    {
            //        MessageBox.Show("Unknown error", "Gantt Chart", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        e.Cancel = true;
            //    }
            //}
            //else if (e.NewValue is TimeSpan)
            //{
            //    if (!m_Manager.SetField((Task)e.RowObject, index, (TimeSpan)e.NewValue))
            //    {
            //        MessageBox.Show("Please try checking datetime input again", "Gantt Chart", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        e.Cancel = true;
            //    }
            //}
            //else if (e.NewValue is DateTime)
            //{
            //    if (!m_Manager.SetField((Task)e.RowObject, index, m_Manager.Start - (DateTime)e.NewValue))
            //    {
            //        MessageBox.Show("Please try checking datetime input again", "Gantt Chart", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //        e.Cancel = true;
            //    }
            //}
        }

        private void m_TaskList_Collapsed(object sender, TreeBranchCollapsedEventArgs e)
        {
            Task task = (Task)e.Model;
            task.IsCollapsed = true;

            m_Chart.Invalidate();

        }

        private void m_TaskList_Expanded(object sender, TreeBranchExpandedEventArgs e)
        {
            Task task = (Task)e.Model;
            task.IsCollapsed = false;

            m_Chart.Invalidate();

        }

        private void m_Chart_TaskMouseDoubleClick(object sender, TaskMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !_searchMode)
            {
                // this check happens BEFORE IsCollapsed is toggled by double click
                if (!e.Task.IsCollapsed)
                {
                    m_TaskList.Collapse(e.Task);
                }
                else
                {
                    m_TaskList.Expand(e.Task);
                }
            }
        }

        private void m_TaskList_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll && !_isPanning)
            {
                m_Chart.Viewport.Y = (e.NewValue + _scrollFix) * m_Chart.BarSpacing;
            }

            _scrollFix = 0;
        }

        private void m_Chart_MouseWheel(object sender, MouseEventArgs e)
        {
            int delta;
            if (e.Delta > 0)
            {
                delta = -m_Chart.Viewport.WheelDelta;
                _scrollFix = -1;
            }
            else
            {
                delta = m_Chart.Viewport.WheelDelta;
                _scrollFix = 1;
            }

            m_TaskList.LowLevelScroll(0, delta);
        }

        private void _mChart_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                //float ratio = (float) dataTreeListView1.GetItemCount() * m_Chart.BarSpacing / m_Chart.Viewport.WorldHeight;
                int delta = e.NewValue - e.OldValue;
                m_TaskList.LowLevelScroll(0, delta);
            }

            //int h1 = dataTreeListView1.Height * dataTreeListView1.RowHeight;
            //float h2 = m_Chart.Viewport.WorldHeight;
        }

        private void _GenerateListView(ObjectListView objectListView, List<Header> headerList)
        {
            List<OLVColumn> columnsList = new List<OLVColumn>();

            foreach (Header header in headerList)
            {
                int index = header.Index;
                OLVColumn columnHeader = new OLVColumn();

                columnHeader.DisplayIndex = index;
                columnHeader.Width = (int)header.Size;
                columnHeader.Text = header.Title;
                columnHeader.AspectGetter = delegate (object x) { return m_Manager.GetField((Task)x, index); };
                columnHeader.AspectToStringConverter = delegate (object x) { return m_Manager.FormatData(x); };

                columnHeader.IsVisible = !header.Hidden;

                columnsList.Add(columnHeader);
                objectListView.AllColumns.Add(columnHeader);
            }

            // Add a 16 x 16 imagelist
            //objectListView.SmallImageList = ;
            //objectListView.Columns.Add(new SmallImageList); TODO
            columnsList[2].AspectPutter = delegate (object x, object value) { m_Manager.SetStart((Task)x, (DateTime)value - m_Manager.Start); };
            columnsList[3].AspectPutter = delegate (object x, object value) { m_Manager.SetEnd((Task)x, (DateTime)value - m_Manager.Start); };
            columnsList[4].AspectPutter = delegate (object x, object value) { m_Manager.SetDuration((Task)x, TimeSpan.Parse((string)value)); };

            objectListView.Columns.AddRange(columnsList.Cast<ColumnHeader>().ToArray());
            objectListView.RebuildColumns();
        }

        void m_Chart_TaskSelected(object sender, TaskMouseEventArgs e)
        {
            _mTaskGrid.SelectedObjects = m_Chart.SelectedTasks.Select(x => m_Manager.IsPart(x) ? m_Manager.SplitTaskOf(x) : x).ToArray();
            _mResourceGrid.Items.Clear();
            _mResourceGrid.Items.AddRange(m_Manager.ResourcesOf(e.Task).Select(x => new ListViewItem(((MyResource)x).Name)).ToArray());

            // Change visibility
            optionsPanel.Visible = true;
        }

        void m_Chart_TaskDeselecting(object sender, TaskMouseEventArgs e)
        {
            // Change visibility
            optionsPanel.Visible = false;
            m_Chart.Invalidate();
        }

        void m_Chart_TaskMouseOut(object sender, TaskMouseEventArgs e)
        {
            lblStatus.Text = "";
            m_Chart.Invalidate();
        }

        void m_Chart_TaskMouseOver(object sender, TaskMouseEventArgs e)
        {
            lblStatus.Text = string.Format("{0} to {1}", m_Manager.GetDateTime(e.Task.Start).ToLongDateString(), m_Manager.GetDateTime(e.Task.End).ToLongDateString());
            m_Chart.Invalidate();
        }

        private void _InitExampleUI()
        {
            _mTaskGridView.DataSource = new BindingSource(m_Manager.Tasks, null);
            //mnuFilePrint200.Click += (s, e) => _PrintDocument(2.0f);
            //mnuFilePrint150.Click += (s, e) => _PrintDocument(1.5f);
            //mnuFilePrint100.Click += (s, e) => _PrintDocument(1.0f);
            //mnuFilePrint80.Click += (s, e) => _PrintDocument(0.8f);
            //mnuFilePrint50.Click += (s, e) => _PrintDocument(0.5f);
            //mnuFilePrint25.Click += (s, e) => _PrintDocument(0.25f);
            //mnuFilePrint10.Click += (s, e) => _PrintDocument(0.1f);

            //mnuFileImgPrint100.Click += (s, e) => _PrintImage(1.0f);
            //mnuFileImgPrint50.Click += (s, e) => _PrintImage(0.5f);
            //mnuFileImgPrint10.Click += (s, e) => _PrintImage(0.1f);
        }

        #region Main Menu

        private void mnuFileSave_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var fs = System.IO.File.OpenWrite(dialog.FileName))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        bf.Serialize(fs, m_Manager);
                    }
                }
            }
        }

        private void mnuFileOpen_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (var fs = System.IO.File.OpenRead(dialog.FileName))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        m_Manager = bf.Deserialize(fs) as ProjectManager;
                        if (m_Manager == null)
                        {
                            MessageBox.Show("Unable to load ProjectManager. Data structure might have changed from previous verions", "Gantt Chart", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            m_Chart.Init(m_Manager);
                            m_Chart.Invalidate();
                            //m_Tasklist.Invalidate();
                        }
                    }
                }
            }
        }

        private void mnuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void mnuViewDaysDayOfWeek_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Week;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            // start a new Project and init the chart with the project
            m_Manager = new ProjectManager("New Project");
            m_Manager.Add(new MyTask(m_Manager) { Name = "New Task" });

            // restart chart
            m_Chart.Init(m_Manager);
            m_Chart.CreateTaskDelegate = delegate () { return new MyTask(m_Manager); };

            m_Chart.Invalidate();

            // restart tasklist
            m_TaskList.Reset();
            m_TaskList.Roots = m_Manager.RootTasks;
            _GenerateListView(m_TaskList, m_Manager.HeaderList);
        }

        private void mnuHelpAbout_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show("Please visit http://www.jakesee.com/net-c-winforms-gantt-chart-control/ for more help and details", "Braincase Solutions - Gantt Chart", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            //{
            //    System.Diagnostics.Process.Start("http://www.jakesee.com/net-c-winforms-gantt-chart-control/");
            //}
            MessageBox.Show("Braincase Solutions - Gantt Chart\nPress help to visit https://github.com/jakesee/ganttchart", "About", MessageBoxButtons.OK, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
                0, // 0 is default otherwise use MessageBoxOptions Enum
                "https://github.com/jakesee/ganttchart",
                "");
        }

        private void mnuViewRelationships_Click(object sender, EventArgs e)
        {
            m_Chart.ShowRelations = mnuViewRelationships.Checked = !mnuViewRelationships.Checked;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void mnuViewSlack_Click(object sender, EventArgs e)
        {
            m_Chart.ShowSlack = mnuViewSlack.Checked = !mnuViewSlack.Checked;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void mnuViewIntructions_Click(object sender, EventArgs e)
        {
            m_Overlay.PrintMode = !(mnuViewIntructions.Checked = !mnuViewIntructions.Checked);
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        #region Timescale Views
        private void mnuViewDays_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Day;
            _ClearTimeResolutionMenu();
            mnuViewDays.Checked = true;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void mnuViewWeeks_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Week;
            _ClearTimeResolutionMenu();
            mnuViewWeeks.Checked = true;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void mnuViewHours_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Hour;
            _ClearTimeResolutionMenu();
            mnuViewHours.Checked = true;
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void _ClearTimeResolutionMenu()
        {
            mnuViewDays.Checked = false;
            mnuViewWeeks.Checked = false;
            mnuViewHours.Checked = false;
        }
        #endregion Timescale Views

        #endregion Main Menu

        #region CustomView

        private void _mDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            m_Manager.Start = _mStartDatePicker.Value;
            var span = DateTime.Today - m_Manager.Start;
            m_Manager.Now = span;

            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void _mPropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void _mNowDatePicker_ValueChanged(object sender, EventArgs e)
        {
            TimeSpan span = _mNowDatePicker.Value - _mStartDatePicker.Value;
            m_Manager.Now = span.Add(new TimeSpan(1, 0, 0, 0));
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void _mScrollDatePicker_ValueChanged(object sender, EventArgs e)
        {
            m_Chart.ScrollTo(_mScrollDatePicker.Value);
            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();
        }

        private void _mTaskGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (_mTaskGridView.SelectedRows.Count > 0)
            {
                var task = _mTaskGridView.SelectedRows[0].DataBoundItem as Task;
                m_Chart.ScrollTo(task);
            }
        }

        private void UpdateTreeView()
        {
            _mTaskTreeView.UseWaitCursor = true; // display a wait cursor while the treenodes are being created.
            _mTaskTreeView.BeginUpdate(); // suppress repainting the treeview until all the objects have been created.

            // clear the treeview each time the method is called.
            _mTaskTreeView.Nodes.Clear();

            // add a root treenode for each customer object in the arraylist.
            foreach (Task task in m_Manager.RootTasks)
            {
                TreeNode node = new TreeNode(getName(task));
                _mTaskTreeView.Nodes.Add(node);

                // if the node is a group, add its children too
                if (m_Manager.IsGroup(task))
                {
                    foreach (Task child in m_Manager.DirectMembersOf(task))
                    {
                        node.Nodes.Add(new TreeNode(getName(child)));
                    }
                }
            }

            _mTaskTreeView.EndUpdate(); // begin repainting the treeview.
            _mTaskTreeView.UseWaitCursor = false; // reset the cursor to the default for all controls.
        }

        private string getName(Task task)
        {
            if (task.Name == null)
            {
                return "(No name)";
            }

            return task.Name;
        }

        #endregion CustomView

        #region Print

        //private void _PrintDocument(float scale)
        //{
        //    using (var dialog = new PrintDialog())
        //    {
        //        dialog.Document = new System.Drawing.Printing.PrintDocument();
        //        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        {
        //            // set the print mode for the custom overlay painter so that we skip printing instructions
        //            dialog.Document.BeginPrint += (s, arg) => _mOverlay.PrintMode = true;
        //            dialog.Document.EndPrint += (s, arg) => _mOverlay.PrintMode = false;

        //            // tell chart to print to the document at the specified scale
        //            m_Chart.Print(dialog.Document, scale);
        //        }
        //    }
        //}

        //private void _PrintImage(float scale)
        //{
        //    using (var dialog = new SaveFileDialog())
        //    {
        //        dialog.Filter = "Bitmap (*.bmp) | *.bmp";
        //        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        {
        //            // set the print mode for the custom overlay painter so that we skip printing instructions
        //            _mOverlay.PrintMode = true;
        //            // tell chart to print to the document at the specified scale

        //            var bitmap = m_Chart.Print(scale);
        //            _mOverlay.PrintMode = false; // restore printing overlays

        //            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Bmp);
        //        }
        //    }
        //}


        #endregion Print        

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (m_Chart.SelectedTask != null)
            {
                if (MessageBox.Show("Press OK to confirm", "Delete", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
                {
                    m_Manager.Delete(m_Chart.SelectedTask);
                    m_Chart.Invalidate();
                    //m_Tasklist.Invalidate();
                }
            }
            else
            {
                MessageBox.Show("No task selected", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var newTask = new MyTask(m_Manager);
            newTask.Name = "New Task";
            var selected = m_Chart.SelectedTask;

            m_Manager.Add(newTask);
            try
            {
                m_Manager.SetStart(newTask, selected.Start);
            }
            catch (NullReferenceException)
            {
                m_Manager.SetStart(newTask, new TimeSpan(0, 0, 0, 0)); // set at 0 instead
            }

            m_Manager.SetDuration(newTask, new TimeSpan(5, 0, 0, 0));

            if (m_Manager.IsPart(selected)) m_Manager.Move(newTask, m_Manager.IndexOf(m_Manager.SplitTaskOf(selected)) + 1 - m_Manager.IndexOf(newTask));
            else m_Manager.Move(newTask, m_Manager.IndexOf(selected) + 1 - m_Manager.IndexOf(newTask));

            m_Chart.Invalidate();
            //m_Tasklist.Invalidate();

            //if (_mProject.IsPart(e.Task)) _mProject.Move(newtask, _mProject.IndexOf(_mProject.SplitTaskOf(e.Task)) + 1 - _mProject.IndexOf(newtask));
            //else _mProject.Move(newtask, _mProject.IndexOf(e.Task) + 1 - _mProject.IndexOf(newtask));
        }

        private void taskTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (taskTabControl.SelectedIndex)
            {
                case 0:
                    // task list dataGrid
                    taskForm.Size = new System.Drawing.Size(800, taskForm.Height);
                    break;
                case 1:
                    // task tree dataGrid
                    UpdateTreeView();
                    taskForm.Size = new System.Drawing.Size(400, taskForm.Height);
                    break;
                case 2:
                    // timeline groupBoxes
                    taskForm.Size = new System.Drawing.Size(400, taskForm.Height);
                    break;
            }
        }

        private void _mTaskTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = _mTaskTreeView.SelectedNode;

            foreach (Task task in m_Manager.Tasks)
            {
                if (node.Text.Equals(task.Name))
                {
                    m_Chart.ScrollTo(task);
                }
            }
        }

        private void taskListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openTaskTabMenu(0);
            taskForm.Size = new System.Drawing.Size(800, taskForm.Height);
        }

        private void taskTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openTaskTabMenu(1);
            taskForm.Size = new System.Drawing.Size(400, taskForm.Height);
        }

        private void timelineToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openTaskTabMenu(2);
            taskForm.Size = new System.Drawing.Size(400, taskForm.Height);
        }

        private void openTaskTabMenu(int index)
        {
            taskTabControl.SelectedIndex = index;
            taskForm.Show();
        }

        private void TaskForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                taskForm.Hide();
            }
            else
            {
                taskForm.Close();
            }
        }

        private void editFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = 0;
            foreach (OLVColumn column in m_TaskList.AllColumns)
            {
                if (column == m_SelectedTaskColumn)
                {
                    break;
                }

                index++;
            }
            var task = m_SelectedTask;

            string fieldName = m_Manager.HeaderList[index].Title;
            string data = m_Manager.GetFieldString(task, index);

            string input;
            if (_promptString("Change value of field [" + fieldName + "]:", "Edit task", data, "Task name cannot be empty", out input))
            {
                if (index >= 4)
                {
                    // Edit custom field
                    task.CustomFieldsData[index - 4] = input;
                }
                else if (index == 0)
                {
                    task.Name = input;
                }
                else if (index == 3)
                {
                    TimeSpan timestamp = new TimeSpan();
                    if (TimeSpan.TryParseExact(input, @"dd\.hh\:mm\:ss", null, System.Globalization.TimeSpanStyles.None, out timestamp))
                    {
                        m_Manager.SetDuration(task, timestamp);
                    }
                    else
                    {
                        MessageBox.Show("Conversion of input string to timespan failed", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    DateTime timestamp = m_Manager.Start;
                    if (DateTime.TryParseExact(input, "yyyy.MM.dd hh:mm:ss", null, System.Globalization.DateTimeStyles.None, out timestamp))
                    {
                        switch (index)
                        {
                            case 1:
                                m_Manager.SetStart(task, timestamp.Subtract(m_Manager.Start));
                                break;
                            case 2:
                                m_Manager.SetEnd(task, timestamp.Subtract(m_Manager.Start));
                                break;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Conversion of input string to timespan failed", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            m_Chart.Invalidate();
            m_TaskList.Invalidate();
        }

        private void addChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!m_Manager.IsSplit(m_SelectedTask))
            {
                Task newTask = new MyTask(m_Manager);
                newTask.Name = "Child of " + m_SelectedTask.Name;
                m_Manager.Add(newTask);
                m_Manager.SetStart(newTask, m_SelectedTask.Start);
                m_Manager.SetDuration(newTask, m_SelectedTask.Duration);

                m_Manager.Group(m_SelectedTask, newTask);

                // Set properties for tasklist
                m_SelectedTask.CanExpand = true;
                m_SelectedTask.Children = m_Manager.MembersOf(m_SelectedTask);
                m_TaskList.Expand(m_SelectedTask);

                m_Chart.Invalidate();
                m_TaskList.RebuildAll(true);
                //m_TaskList.ExpandAll();
            }
            else
            {
                MessageBox.Show("Cannot create child task for split task", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Press OK to confirm", "Delete task", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                m_Manager.Delete(m_SelectedTask);

                m_TaskList.Roots = m_Manager.RootTasks;

                m_Chart.Invalidate();
            }
        }

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_Manager.Merge(m_SelectedTask);
        }

        private void splitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if(_mManager.IsSplit(_mChart.SelectedTask))
            //{
            //    _mManager.Split(_mChart.SelectedTask, _mManager.SplitTaskOf(_mChart.SelectedTask), new MyTask(_mManager), new TimeSpan(_mManager.SplitTaskOf(_mChart.SelectedTask).Duration.Ticks / 2));
            //}
            //else
            //{
            //    _mManager.Split(_mChart.SelectedTask, , );
            //}

            var parts = m_Manager.PartsOf(m_SelectedTask);
            int splitTasks = parts.Count() - 1;


            if (splitTasks > 0)
                m_Manager.Split(parts.First(), new MyTask(m_Manager), new TimeSpan(parts.First().Duration.Ticks / 2 ^ (splitTasks - 1)));
            else
                m_Manager.Split(m_SelectedTask, new MyTask(m_Manager), new MyTask(m_Manager), new TimeSpan(m_SelectedTask.Duration.Ticks / 2));

            m_Chart.Invalidate();
        }

        private void editProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string result;
            if (_promptString("Enter a new project name:", "Edit project", m_Manager.Name, "Project name cannot be empty", out result))
            {
                m_Manager.Name = result;
                projectTitleLabel.Text = result;
                m_Chart.Invalidate();
                m_TaskList.Invalidate();
            }

        }
        private void resizeHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = 0; // m_Tasklist.SelectedFieldIndex;
            string header = m_Manager.HeaderList[index].Title;

            string input;
            if (_promptString("Enter a new size:", "Edit header", m_Manager.HeaderList[index].Size.ToString(), "Enter a valid number!", out input))
            {
                float size = float.Parse(input);

                if (m_Manager.FieldMinSize > size || m_Manager.FieldMaxSize < size)
                {
                    MessageBox.Show("Field size should be between " + m_Manager.FieldMinSize + " and " + m_Manager.FieldMaxSize, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    m_Manager.HeaderList[index].Size = size;
                }
                //m_Tasklist.RecalculateTasklist();
                //m_Tasklist.Invalidate();
            }

        }

        private void editHeaderNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = 0; // m_Tasklist.SelectedFieldIndex;
            string header = m_Manager.HeaderList[index].Title;

            string input;
            if (_promptString("Change name of header [" + header + "] to:", "Edit header", header, "Task name cannot be empty", out input))
            {
                m_Manager.HeaderList[index].Title = input;
                // m_Tasklist.Invalidate(); // in case of change in text alignment
            }
        }

        private void createCustomFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            if (_promptString("Enter a new custom field:", "Create custom field", "", "Task name cannot be empty", out input))
            {
                _registerCustomField(input, "string", 80f);
                //m_Tasklist.RecalculateTasklist();
                //m_Tasklist.Invalidate();
            }
        }

        private void deleteCustomFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int input;
            if (_promptList("Select a field to delete", "Delete custom field", m_Manager.GetHeaderNames(), out input))
            {
                if (input >= m_Manager.MainFieldCount)
                {
                    _deregisterCustomField(input);
                    //m_Tasklist.RecalculateTasklist();
                    //m_Tasklist.Invalidate();
                }
                else
                {
                    MessageBox.Show("Cannot delete important field", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {

            if (m_SearchTextBox.Focused)
            {
                _SearchTasksWordsInTextBox();
            }

        }

        private void _SearchTasksWordsInTextBox()
        {
            if (!m_SearchTextBox.Text.Equals("Search..."))
            {
                if (m_SearchTextBox.Text.Length == 0)
                {
                    foreach (Task task in m_Manager.Tasks)
                    {
                        task.IsFiltered = false;
                    }

                    m_TaskList.ModelFilter = null;
                }
                else
                {
                    var filters = new List<IModelFilter>();
                    var words = m_SearchTextBox.Text.Trim().Split(null);
                    TextMatchFilter highlightingFilter = TextMatchFilter.Contains(m_TaskList, words); ;

                    foreach (var word in words)
                    {
                        var filter = TextMatchFilter.Contains(m_TaskList, word);
                        filters.Add(filter);
                    }

                    var compositeFilter = new CompositeAllFilter(filters);

                    m_TaskList.ModelFilter = highlightingFilter;
                    m_TaskList.AdditionalFilter = compositeFilter;
                    m_TaskList.DefaultRenderer = new HighlightTextRenderer(highlightingFilter);

                    // Update isFiltered property for all tasks
                    foreach (Task task in m_Manager.Tasks)
                    {
                        task.IsFiltered = !highlightingFilter.Filter(task);

                        if (m_Manager.IsGroup(task))
                        {
                            task.IsFiltered = true;
                        }
                        else
                        {
                            // Update parent's filtered depending on its children
                            Task parent = task;
                            while (m_Manager.IsMember(parent))
                            {
                                parent = m_Manager.DirectGroupOf(parent);

                                if (!parent.IsCollapsed)
                                {
                                    parent.IsFiltered = parent.IsFiltered && task.IsFiltered;
                                }
                            }
                        }
                    }
                }
            }

            m_Chart.Invalidate();
        }

        private void _registerCustomField(string fieldName, string type, float fieldSize)
        {
            // Add field to projectmanager
            m_Manager.AddCustomField(fieldName, type, fieldSize);

            int index = m_Manager.FieldCount - 1;
            Header header = m_Manager.HeaderList[index];

            // Add field to tasklist
            OLVColumn columnHeader = new OLVColumn();

            columnHeader.DisplayIndex = index;
            columnHeader.Width = (int)header.Size;
            columnHeader.Text = header.Title;
            columnHeader.AspectGetter = delegate (object x) { return m_Manager.GetField((Task)x, index); };
            columnHeader.AspectToStringConverter = delegate (object x) { return m_Manager.FormatData(x); };
            columnHeader.AspectPutter = delegate (object x, object value)
            {
                m_Manager.SetField((Task)x, index, (string)value);
            };
            columnHeader.IsVisible = !header.Hidden;

            m_TaskList.AllColumns.Add(columnHeader);
            m_TaskList.RebuildColumns();
        }

        private void _deregisterCustomField(int index)
        {
            m_Manager.RemoveCustomField(index);

            m_TaskList.AllColumns.RemoveAt(index);
            m_TaskList.RebuildColumns();
        }

        private bool _promptString(string question, string caption, string oldString, string errorMsg, out string result)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 140,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label()
            {
                Left = 10,
                Top = 10,
                Text = question,
                Size = new Size(265, 20)
            };
            TextBox textBox = new TextBox()
            {
                Left = 10,
                Top = 30,
                Width = 265,
                Text = oldString
            };
            Button confirmation = new Button() { Text = "Ok", Left = 90, Width = 100, Top = 65, DialogResult = DialogResult.OK, };
            confirmation.Click += (senderP, eP) =>
                    {
                        prompt.Close();
                    };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                if (textBox.Text.Length == 0 && errorMsg.Length != 0)
                {
                    MessageBox.Show(errorMsg, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return _promptString(question, caption, oldString, errorMsg, out result);
                }
                else
                {
                    result = textBox.Text;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool _promptList(string question, string caption, List<string> entries, out int choice)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 250,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent
            };
            Label textLabel = new Label()
            {
                Left = 10,
                Top = 10,
                Text = question,
                Size = new Size(265, 20)
            };
            ListBox listBox = new ListBox()
            {
                Left = 10,
                Top = 30,
                Size = new Size(265, 130)
            };
            Button confirmation = new Button()
            {
                Text = "Ok",
                Left = 90,
                Width = 100,
                Top = 170,
                DialogResult = DialogResult.OK,
            };
            confirmation.Click += (senderP, eP) =>
            {
                prompt.Close();
            };

            listBox.Items.AddRange(entries.ToArray());

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(listBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            if (prompt.ShowDialog() == DialogResult.OK)
            {
                if (listBox.SelectedIndex != -1)
                {
                    choice = listBox.SelectedIndex;
                    return true;
                }
            }

            choice = -1;
            return false;
        }

        private bool _TaskContainsKeywords(Task task, string term)
        {
            bool contains = false;
            for (int i = 0; i < m_Manager.FieldCount; i++)
            {
                foreach (string keyword in term.Trim().Split(null))
                {
                    string data = m_Manager.GetFieldString(task, i);

                    if (data != null)
                    {
                        contains = contains || CultureInfo.CurrentCulture.CompareInfo.IndexOf(data, term, CompareOptions.IgnoreCase) >= 0;
                    }
                }
            }

            return contains;
        }

        private void showAllHeadersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (OLVColumn column in m_TaskList.AllColumns)
            {
                column.IsVisible = true;
            }

            m_TaskList.RebuildColumns();
            //m_TaskList.Invalidate();
        }

        private void editFieldToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            int input;
            if (_promptList("Select a field to edit", "Edit custom field", m_Manager.GetHeaderNames(), out input))
            {
                string header = m_Manager.HeaderList[input].Title;

                string input2;
                if (_promptString("Change name of header [" + header + "] to:", "Edit header", header, "Task name cannot be empty", out input2))
                {
                    m_Manager.HeaderList[input].Title = input2;
                    m_TaskList.AllColumns[input].Text = input2;
                    m_TaskList.RebuildColumns();
                }
            }
        }

        private void setDelayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            if (_promptString("Enter new delay", "Set delay", m_SelectedTask.Delay.ToString("g"), "Wrong input", out input))
            {
                m_Manager.SetDelay(m_SelectedTask, TimeSpan.Parse(input));
                m_TaskList.Invalidate();
                m_Chart.Invalidate();
            }
        }

        private void toggleSmartViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_Chart.smartView = toggleSmartViewToolStripMenuItem.Checked = !toggleSmartViewToolStripMenuItem.Checked;
            m_Chart.Invalidate();
        }
    }

    #region overlay painter
    /// <summary>
    /// An example of how to encapsulate a helper painter for painter additional features on Chart
    /// </summary>
    public class OverlayPainter
    {
        /// <summary>
        /// Hook such a method to the chart paint event listeners
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ChartOverlayPainter(object sender, ChartPaintEventArgs e)
        {
            // Don't want to print instructions to file
            if (this.PrintMode) return;

            var g = e.Graphics;
            var chart = e.Chart;

            // Demo: Static billboards begin -----------------------------------
            // Demonstrate how to draw static billboards
            // "push matrix" -- save our transformation matrix
            e.Chart.BeginBillboardMode(e.Graphics);

            // draw mouse command instructions
            int margin = 320;
            int left = 20;
            var color = chart.HeaderFormat.Color;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("LEFT  - Select task and display properties in PropertyGrid");
            builder.AppendLine("LEFT Doubleclick  - Toggle collaspe on task group");
            builder.AppendLine("");
            builder.AppendLine("LEFT + Drag  - Change task starting point");
            builder.AppendLine("RIGHT + Drag  - Change task duration");
            builder.AppendLine("MIDDLE + Drag  - Change task complete percentage");
            builder.AppendLine("RIGHT Doubleclick  - Split task into task parts");
            builder.AppendLine("LEFT + Drag and drop*  - Group drag task under drop task");
            builder.AppendLine("RIGHT + Drag and drop*  - Join task parts");
            builder.AppendLine("SHIFT + LEFT + Drag and drop*  - Make drop task precedent of drag task");
            builder.AppendLine("ALT + LEFT + Drag and drop*  - Ungroup drag task from drop task / Remove drop task from drag task precedent list");
            builder.AppendLine("");
            builder.AppendLine("SHIFT + LEFT + Drag and drop*  - Order tasks");
            builder.AppendLine("SHIFT + MIDDLE  - Create new task");
            builder.AppendLine("ALT + MIDDLE  - Delete task");
            builder.AppendLine("LEFT Doubleclick  - Toggle collaspe on task group");
            builder.AppendLine();
            builder.AppendLine("*Drag and drop onto another task or task part");
            var size = g.MeasureString(builder.ToString(), e.Chart.Font);
            var background = new Rectangle(left, chart.Height - margin, (int)size.Width, (int)size.Height);
            background.Inflate(10, 10);
            g.FillRectangle(new System.Drawing.Drawing2D.LinearGradientBrush(background, Color.LightYellow, Color.White, System.Drawing.Drawing2D.LinearGradientMode.Vertical), background);
            g.DrawRectangle(Pens.Brown, background);
            g.DrawString(builder.ToString(), chart.Font, color, new PointF(left, chart.Height - margin));


            // "pop matrix" -- restore the previous matrix
            e.Chart.EndBillboardMode(e.Graphics);
            // Demo: Static billboards end -----------------------------------
        }

        public bool PrintMode { get; set; }
    }
    #endregion overlay painter

    #region custom task and resource
    /// <summary>
    /// A custom resource of your own type (optional)
    /// </summary>
    [Serializable]
    public class MyResource
    {
        public string Name { get; set; }
    }
    /// <summary>
    /// A custom task of your own type deriving from the Task interface (optional)
    /// </summary>
    [Serializable]
    public class MyTask : Task
    {
        public MyTask(ProjectManager manager)
            : base()
        {
            Manager = manager;
        }

        private ProjectManager Manager { get; set; }
        public new float Complete { get { return base.Complete; } set { Manager.SetComplete(this, value); } }

        /// <summary>
        /// Convert this Task to a descriptive string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = string.Format("[Name: {0}, Start: {1}, End: {2}, Actual End: {3}, Delay: {4}, Duration: {5}, Complete: {6}, CanExpand: {7}, IsCollapsed: {8}, IsFiltered: {9}, UserFields: ", Name, Start, End, ActualEnd, Delay, Duration, Complete, CanExpand, IsCollapsed, IsFiltered);

            for (int i = 0; i < CustomFieldsData.Length - 1; i++)
            {
                str += CustomFieldsData[i] + ", ";
            }
            str += CustomFieldsData[CustomFieldsData.Length - 1] + "]";

            return str;
        }
    }
    #endregion custom task and resource


}
