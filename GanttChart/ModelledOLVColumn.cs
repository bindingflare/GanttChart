using BrightIdeasSoftware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edcore.GanttChart
{
    class DataFormats
    {
        public static string DateTimeFormat = "{0:yyyy.MM.dd hh:mm:ss}";
        public static string TimeSpanFormat = @"{0:dd\.hh\:mm\:ss}";
        public static string PercentFormat = "{0:p}";
    }

    public class ModelledOLVColumn : OLVColumn
    {
        public object DummyValue { get; set; }

        public ModelledOLVColumn(int displayIndex, int width, string text, bool isVisible, object dummyValue) : this(displayIndex, width, text, true, isVisible, dummyValue)
        {
        }

        public ModelledOLVColumn(int displayIndex, int width, string text, bool isEditable, bool isVisible, object dummyValue)
        {
            DisplayIndex = displayIndex;
            Width = width;
            Text = text;
            IsEditable = isEditable;
            IsVisible = isVisible;
            DummyValue = dummyValue;
        }
    }

    class NameColumn : ModelledOLVColumn
    {
        public NameColumn(int displayIndex, int width) : base(displayIndex, width, "Name", true, "")
        {
            AspectGetter = delegate (object x) { return ((Task)x).Name; };
            // Use base aspect putter
            // Use base aspect to string format
        }
    }

    class IDColumn : ModelledOLVColumn
    {
        public IDColumn(int displayIndex, int width, bool isVisible) : base(displayIndex, width, "ID", isVisible, "")
        {
            AspectGetter = delegate (object x) { return ((Task)x).ID; };
            // Use base aspect putter
            // Use base aspect to string format
        }
    }

    class StartColumn : ModelledOLVColumn
    {
        private ProjectManager<Task, object> m_Manager;

        public StartColumn(ProjectManager<Task, object> project, int displayIndex, int width, bool isVisible) : base(displayIndex, width, "Start", isVisible, TimeSpan.Zero)
        {
            m_Manager = project;
            AspectGetter = delegate (object x) { return m_Manager.GetDateTime(((Task)x).Start); };
            AspectPutter = delegate (object x, object value) { m_Manager.SetStart((Task)x, (DateTime)value - m_Manager.Start); };
            AspectToStringFormat = DataFormats.DateTimeFormat;
        }
    }

    class EndColumn : ModelledOLVColumn
    {
        private ProjectManager<Task, object> m_Manager;

        public EndColumn(ProjectManager<Task, object> project, int displayIndex, int width, bool isVisible) : base(displayIndex, width, "End", isVisible, TimeSpan.Zero)
        {
            m_Manager = project;
            AspectGetter = delegate (object x) { return m_Manager.GetDateTime(((Task)x).End); };
            AspectPutter = delegate (object x, object value) { m_Manager.SetEnd((Task)x, (DateTime)value - m_Manager.Start); };
            AspectToStringFormat = DataFormats.DateTimeFormat;
        }
    }

    class DurationColumn : ModelledOLVColumn
    {
        private ProjectManager<Task, object> m_Manager;

        public DurationColumn(ProjectManager<Task, object> project, int displayIndex, int width, bool isVisible) : base(displayIndex, width, "Duration", isVisible, TimeSpan.Zero)
        {
            m_Manager = project;
            AspectGetter = delegate (object x) { return ((Task)x).Duration; };
            AspectPutter = delegate (object x, object value) { m_Manager.SetDuration((Task)x, (TimeSpan)value); };
            AspectToStringFormat = DataFormats.TimeSpanFormat;
        }
    }

    class CompleteColumn : ModelledOLVColumn
    {
        private ProjectManager<Task, object> m_Manager;

        public CompleteColumn(ProjectManager<Task, object> project, int displayIndex, int width, bool isVisible) : base(displayIndex, width, "Complete", isVisible, 0f)
        {
            m_Manager = project;
            AspectGetter = delegate (object x) { return ((Task)x).Complete; };
            AspectPutter = delegate (object x, object value) { m_Manager.SetComplete((Task)x, Convert.ToSingle((double)value)); };
            AspectToStringFormat = DataFormats.PercentFormat;
        }
    }

    class DelayColumn : ModelledOLVColumn
    {
        private ProjectManager<Task, object> m_Manager;

        public DelayColumn(ProjectManager<Task, object> project, int displayIndex, int width, bool isVisible) : base(displayIndex, width, "Delay", isVisible, TimeSpan.Zero)
        {
            m_Manager = project;
            AspectGetter = delegate (object x) { return ((Task)x).Delay; };
            AspectPutter = delegate (object x, object value) { m_Manager.SetDelay((Task)x, (TimeSpan)value); };
            AspectToStringFormat = DataFormats.TimeSpanFormat;
        }
    }

    class CustomColumn : ModelledOLVColumn
    {
        private int _CustomFieldIndex;

        public CustomColumn(int displayIndex, int width, string name, string type, int customFieldIndex, bool isVisible, object dummyValue) : base(displayIndex, width, name, isVisible, dummyValue)
        {
            _CustomFieldIndex = customFieldIndex;
            
            if(type == "string")
            {
                AspectGetter = delegate (object x) { return ((Task)x).CustomFieldsData[customFieldIndex]; };
                AspectPutter = delegate (object x, object value) { ((Task)x).CustomFieldsData[customFieldIndex] = (string)value; };
            }
            else if(type == "checkbox")
            {
                CheckBoxes = true;

                if (bool.Parse((string)dummyValue))
                {
                    DummyValue = "True";  
                }
                else
                {
                    DummyValue = "False";
                }

                AspectGetter = delegate (object x) {
                    return bool.Parse(((Task)x).CustomFieldsData[customFieldIndex]);
                };
                AspectPutter = delegate (object x, object value) {
                    //PutCheckState(x, System.Windows.Forms.CheckState.Checked);
                    if(value is bool)
                    {
                        value = value.ToString();
                    }

                    ((Task)x).CustomFieldsData[customFieldIndex] = (string)value;
                };
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
