using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        OverlayPainter _mOverlay = new OverlayPainter();

        ProjectManager m_Manager = null;

        Form taskForm = null;
        

        /// <summary>
        /// Example starts here
        /// </summary>
        public NewUI()
        {
            InitializeComponent();

            // Create a Project and some Tasks
            m_Manager = new ProjectManager("256K DRAM 7월 진척현황");
            m_Manager.AddCustomField("Important", "string", 80.0f);
            m_Manager.AddCustomField("Cancelled", "string", 80.0f);

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

            m_Manager.SetCustomField(work, 0, "Hello");
            m_Manager.SetCustomField(wake, 0, "Hello");
            m_Manager.SetCustomField(teeth, 0, "Hello");
            m_Manager.SetCustomField(shower, 0, "Hello");
            m_Manager.SetCustomField(clothes, 0, "Hello");
            m_Manager.SetCustomField(hair, 0, "Hello");
            m_Manager.SetCustomField(pack, 0, "Hello");

            m_Manager.SetCustomField(work, "Important", "Yes");

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

            // demostrate splitting a task
            m_Manager.Split(pack, new MyTask(m_Manager), new MyTask(m_Manager), TimeSpan.FromDays(2));

            // Set task complete status, e.g. using newly created properties
            wake.Complete = 0.9f;
            teeth.Complete = 0.5f;
            shower.Complete = 0.4f;

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

            // Create and assign Resources.
            // MyResource is just custom user class. The API can accept any object as resource.
            var jake = new MyResource() { Name = "Jake" };
            var peter = new MyResource() { Name = "Peter" };
            var john = new MyResource() { Name = "John" };
            var lucas = new MyResource() { Name = "Lucas" };
            var james = new MyResource() { Name = "James" };
            var mary = new MyResource() { Name = "Mary" };
            // Add some resources
            m_Manager.Assign(wake, jake);
            m_Manager.Assign(wake, peter);
            m_Manager.Assign(wake, john);
            m_Manager.Assign(teeth, jake);
            m_Manager.Assign(teeth, james);
            m_Manager.Assign(pack, james);
            m_Manager.Assign(pack, lucas);
            m_Manager.Assign(shower, mary);
            m_Manager.Assign(shower, lucas);
            m_Manager.Assign(shower, john);

            // Initialize the Chart with our ProjectManager and CreateTaskDelegate
            m_Chart.Init(m_Manager);
            m_Chart.CreateTaskDelegate = delegate () { return new MyTask(m_Manager); };

            // Initialize the Tasklist with our ProjectManager and Chart
            m_Tasklist.Init(m_Manager, m_Chart);

            // Attach event listeners for events we are interested in
            m_Chart.TaskMouseOver += new EventHandler<TaskMouseEventArgs>(_mChart_TaskMouseOver);
            m_Chart.TaskMouseOut += new EventHandler<TaskMouseEventArgs>(_mChart_TaskMouseOut);
            m_Chart.TaskSelected += new EventHandler<TaskMouseEventArgs>(_mChart_TaskSelected);
            m_Chart.TaskDeselecting += new EventHandler<TaskMouseEventArgs>(_mChart_TaskDeselecting);
            _mOverlay.PrintMode = true;
            m_Chart.PaintOverlay += _mOverlay.ChartOverlayPainter;
            m_Chart.AllowTaskDragDrop = true;

            m_Tasklist.TaskListMouseClick += new EventHandler<TaskListMouseEventArgs>(_mChart_TaskListMouseClick);

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

        private void _mChart_TaskListMouseClick(object sender, TaskListMouseEventArgs e)
        {
            if (e.Task != null)
            {
                if (e.Button == MouseButtons.Right)
                {
                    taskMenuStrip.Show(this, new Point(e.X, e.Y));
                }
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    headerMenuStrip.Show(this, new Point(e.X, e.Y));
                }
            }

        }

        void _mChart_TaskSelected(object sender, TaskMouseEventArgs e)
        {
            _mTaskGrid.SelectedObjects = m_Chart.SelectedTasks.Select(x => m_Manager.IsPart(x) ? m_Manager.SplitTaskOf(x) : x).ToArray();
            _mResourceGrid.Items.Clear();
            _mResourceGrid.Items.AddRange(m_Manager.ResourcesOf(e.Task).Select(x => new ListViewItem(((MyResource)x).Name)).ToArray());

            // Change visibility
            optionsPanel.Visible = true;
        }

        void _mChart_TaskDeselecting(object sender, TaskMouseEventArgs e)
        {
            // Change visibility
            optionsPanel.Visible = false;
            m_Chart.Invalidate();
        }

        void _mChart_TaskMouseOut(object sender, TaskMouseEventArgs e)
        {
            lblStatus.Text = "";
            m_Chart.Invalidate();
        }

        void _mChart_TaskMouseOver(object sender, TaskMouseEventArgs e)
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
                            m_Tasklist.Invalidate();
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
            m_Tasklist.Invalidate();
        }

        private void mnuFileNew_Click(object sender, EventArgs e)
        {
            // start a new Project and init the chart with the project
            m_Manager = new ProjectManager("New Project");
            m_Manager.Add(new Task() { Name = "New Task" });

            m_Chart.Init(m_Manager);
            m_Chart.CreateTaskDelegate = delegate () { return new MyTask(m_Manager); };

            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
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
            m_Tasklist.Invalidate();
        }

        private void mnuViewSlack_Click(object sender, EventArgs e)
        {
            m_Chart.ShowSlack = mnuViewSlack.Checked = !mnuViewSlack.Checked;
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void mnuViewIntructions_Click(object sender, EventArgs e)
        {
            _mOverlay.PrintMode = !(mnuViewIntructions.Checked = !mnuViewIntructions.Checked);
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        #region Timescale Views
        private void mnuViewDays_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Day;
            _ClearTimeResolutionMenu();
            mnuViewDays.Checked = true;
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void mnuViewWeeks_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Week;
            _ClearTimeResolutionMenu();
            mnuViewWeeks.Checked = true;
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void mnuViewHours_Click(object sender, EventArgs e)
        {
            m_Chart.TimeResolution = TimeResolution.Hour;
            _ClearTimeResolutionMenu();
            mnuViewHours.Checked = true;
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void _ClearTimeResolutionMenu()
        {
            mnuViewDays.Checked = false;
            mnuViewWeeks.Checked = false;
            mnuViewHours.Checked = false;
        }
        #endregion Timescale Views

        #endregion Main Menu

        #region Sidebar

        private void _mDateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            m_Manager.Start = _mStartDatePicker.Value;
            var span = DateTime.Today - m_Manager.Start;
            m_Manager.Now = span;

            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void _mPropertyGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void _mNowDatePicker_ValueChanged(object sender, EventArgs e)
        {
            TimeSpan span = _mNowDatePicker.Value - _mStartDatePicker.Value;
            m_Manager.Now = span.Add(new TimeSpan(1, 0, 0, 0));
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
        }

        private void _mScrollDatePicker_ValueChanged(object sender, EventArgs e)
        {
            m_Chart.ScrollTo(_mScrollDatePicker.Value);
            m_Chart.Invalidate();
            m_Tasklist.Invalidate();
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

        #endregion Sidebar

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
                    m_Tasklist.Invalidate();
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
            m_Tasklist.Invalidate();

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
                    UpdateTreeView(); // update TaskTreeView
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
            int index = m_Tasklist.SelectedFieldIndex;
            var task = m_Chart.SelectedTask;

            string fieldName = m_Manager.GetFieldName(index);
            string data = m_Manager.GetData(task, index);

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
            m_Tasklist.Invalidate();
        }

        private void addChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!m_Manager.IsSplit(m_Chart.SelectedTask))
            {
                Task newTask = new MyTask(m_Manager);
                newTask.Name = "Child of " + m_Chart.SelectedTask.Name;
                m_Manager.Add(newTask);
                m_Manager.SetStart(newTask, m_Chart.SelectedTask.Start);
                m_Manager.SetDuration(newTask, m_Chart.SelectedTask.Duration);

                m_Manager.Group(m_Chart.SelectedTask, newTask);
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
                m_Manager.Delete(m_Chart.SelectedTask);
                m_Chart.Invalidate();
                m_Tasklist.Invalidate();
            }
        }

        private void mergeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_Manager.Merge(m_Chart.SelectedTask);
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

            var parts = m_Manager.PartsOf(m_Chart.SelectedTask);
            int splitTasks = parts.Count() - 1;


            if (splitTasks > 0)
                m_Manager.Split(parts.First(), new MyTask(m_Manager), new TimeSpan(parts.First().Duration.Ticks / 2 ^ (splitTasks - 1)));
            else
                m_Manager.Split(m_Chart.SelectedTask, new MyTask(m_Manager), new MyTask(m_Manager), new TimeSpan(m_Chart.SelectedTask.Duration.Ticks / 2));
        }

        private void editProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string result;
            if (_promptString("Enter a new project name:", "Edit project", m_Manager.Name, "Project name cannot be empty", out result))
            {
                m_Manager.Name = result;
                m_Chart.Invalidate();
                m_Tasklist.Invalidate();
            }

        }
        private void resizeHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = m_Tasklist.SelectedFieldIndex;
            string header = m_Manager.GetFieldName(index);

            string input;
            if (_promptString("Enter a new size:", "Edit header", m_Manager.GetFieldSize(index).ToString(), "Enter a valid number!", out input))
            {
                float size = float.Parse(input);
                if(!m_Manager.SetFieldSize(index, size))
                {
                    MessageBox.Show("Field size should be between " + m_Manager.FieldMinSize + " and " + m_Manager.FieldMaxSize, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                m_Tasklist.RecalculateTasklist();
                m_Tasklist.Invalidate();
            }

        }

        private void editHeaderNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = m_Tasklist.SelectedFieldIndex;
            string header = m_Manager.GetFieldName(index);

            string input;
            if (_promptString("Change name of header [" + header + "] to:", "Edit header", header, "Task name cannot be empty", out input))
            {
                m_Manager.SetFieldName(index, input);
                m_Tasklist.Invalidate(); // in case of change in text alignment
            }
        }

        private void hideHeaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = m_Tasklist.SelectedFieldIndex;
            m_Manager.SetFieldHidden(index, true);
            m_Tasklist.RecalculateTasklist();
            m_Tasklist.Invalidate();
        }

        private void headerOrderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int index = m_Tasklist.SelectedFieldIndex;
            int priority = m_Manager.GetFieldPriority(index);
            string header = m_Manager.GetFieldName(index);

            string input;
            if (_promptString("Change priority of header [" + header + "] to:", "Edit header", priority.ToString(), "Enter a valid number", out input))
            {
                int newPriority = int.Parse(input);
                m_Manager.SetFieldPriority(index, newPriority);
                m_Tasklist.RecalculateTasklist();
                m_Tasklist.Invalidate();// in case of change in text alignment
            }
        }


        private void createCustomFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            if (_promptString("Enter a new custom field:", "Create custom field", "", "Task name cannot be empty", out input))
            {
                m_Manager.AddCustomField(input, "string", 80f);
                m_Tasklist.RecalculateTasklist();
                m_Tasklist.Invalidate();
            }
        }

        private void deleteCustomFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int input;
            if (_promptList("Select a field to delete", "Delete custom field", m_Manager.GetHeaderNames(), out input))
            {
                if (input > 3)
                {
                    m_Manager.RemoveCustomField(input);
                    m_Tasklist.RecalculateTasklist();
                    m_Tasklist.Invalidate();
                }
                else
                {
                    MessageBox.Show("Cannot delete important field", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void showHiddenFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<string> hiddenFields = new List<string>();
            for(int index = 0; index < m_Manager.FieldCount; index++)
            {
                if(m_Manager.GetFieldHidden(index))
                {
                    hiddenFields.Add(m_Manager.GetFieldName(index));
                }
            }

            int input;
            if (_promptList("Select a field to show", "Show hidden", hiddenFields, out input))
            {
                // Convert input back to index
                input++;
                int index = 0;

                int hiddenCount = 0;
                for(int i = 0; i < m_Manager.FieldCount; i++)
                {
                    if(m_Manager.GetFieldHidden(i))
                    {
                        hiddenCount++;
                    }

                    if (hiddenCount == input)
                    {
                        index = i;
                        break;
                    }
                }
                

                m_Manager.SetFieldHidden(index, false);
                m_Tasklist.RecalculateTasklist();
                m_Tasklist.Invalidate();
            }
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

        public new TimeSpan Start { get { return base.Start; } set { Manager.SetStart(this, value); } }
        public new TimeSpan End { get { return base.End; } set { Manager.SetEnd(this, value); } }
        public new TimeSpan Duration { get { return base.Duration; } set { Manager.SetDuration(this, value); } }
        public new float Complete { get { return base.Complete; } set { Manager.SetComplete(this, value); } }
    }
    #endregion custom task and resource
}
