using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Reflection;
using System.Drawing;
using System.Drawing.Design;
using Nebulator.Common;


namespace Nebulator.Controls
{
    /// <summary>Extends the PropertyGrid to add some features.</summary>
    public class PropertyGridEx : PropertyGrid
    {
        #region Events
        /// <summary>The property grid is reporting something.</summary>
        public event EventHandler<PropertyGridExEventArgs> PropertyGridExEvent;

        /// <summary>General event for raising events not natively supported by the property grid.</summary>
        public class PropertyGridExEventArgs : EventArgs
        {
            /// <summary>General info.</summary>
            public string EventType { get; set; }

            /// <summary>General data.</summary>
            public object EventData { get; set; }
        }

        /// <summary>Children can call this to send something back to the host.</summary>
        public void RaisePropertyGridExEvent(string eventType, object ps = null)
        {
            PropertyGridExEvent?.Invoke(this, new PropertyGridExEventArgs() { EventType = eventType, EventData = ps });
        }
        #endregion

        #region Properties
        /// <summary>Gets the tool strip.</summary>
        [Category("Appearance"), DisplayName("Toolstrip"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), DescriptionAttribute("Toolbar object"), Browsable(true)]
        public ToolStrip ToolStrip { get { return _toolstrip; } }

        /// <summary>Gets the doc comment.</summary>
        [Category("Appearance"), DisplayName("Doc"), DescriptionAttribute("DocComment object. Represent the comments area of the PropertyGrid."), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Control DocComment { get { return (Control)_docComment; } }

        /// <summary>Gets the doc comment title.</summary>
        [Category("Appearance"), DisplayName("HelpTitle"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), DescriptionAttribute("Doc Title Label."), Browsable(true)]
        public Label DocCommentTitle { get { return _docCommentTitle; } }

        /// <summary>Gets the doc comment description.</summary>
        [Category("Appearance"), DisplayName("DocDescription"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), DescriptionAttribute("Doc Description Label."), Browsable(true)]
        public Label DocCommentDescription { get { return _docCommentDescription; } }

        /// <summary>Gets or sets the help comment image.</summary>
        [Category("Appearance"), DisplayName("DocImageBackground"), DescriptionAttribute("Doc Image Background.")]
        public Image DocCommentImage
        {
            get { return ((Control)_docComment).BackgroundImage; }
            set { ((Control)_docComment).BackgroundImage = value; }
        }

        /// <summary>Edited flag.</summary>
        [Browsable(false)]
        public bool Dirty { get; set; } = false;
        #endregion

        #region Private fields
        // Internal PropertyGrid Controls
        private object _propertyGridView;
        private object _hotCommands;
        private object _docComment;

        private ToolStrip _toolstrip = null;
        private Label _docCommentTitle = null;
        private Label _docCommentDescription = null;
        private FieldInfo _propertyGridEntries = null;
        #endregion

        /// <summary>Initializes a new instance of the class.</summary>
        public PropertyGridEx()
        {
            // Add any initialization after the InitializeComponent() call.
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            //BackColor = Globals.UserSettings.BackColor;
            
            _propertyGridView = base.GetType().BaseType.InvokeMember("gridView", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, this, null);
            _hotCommands = base.GetType().BaseType.InvokeMember("hotcommands", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, this, null);
            _toolstrip = (ToolStrip)base.GetType().BaseType.InvokeMember("toolStrip", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, this, null);
            _docComment = base.GetType().BaseType.InvokeMember("doccomment", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, this, null);
            
            if (_docComment != null)
            {
                _docCommentTitle = (Label)_docComment.GetType().InvokeMember("m_labelTitle", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, _docComment, null);
                _docCommentDescription = (Label)_docComment.GetType().InvokeMember("m_labelDesc", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, _docComment, null);
            }

            if (_propertyGridView != null)
            {
                _propertyGridEntries = _propertyGridView.GetType().GetField("allGridEntries", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }

            PropertyValueChanged += Edit_PropertyValueChanged;

            Dirty = false;
        }

        /// <summary>User edited something.</summary>
        private void Edit_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Dirty = true;
        }

        /// <summary>Add a custom button to the property grid.</summary>
        public void AddButton(string text, Image image, string tooltip, EventHandler onClick)
        {
            foreach (Control control in Controls)
            {
                if (control is ToolStrip)
                {
                    // Found toolstrip - add our stuff.
                    (control as ToolStrip).Items.Add(new ToolStripSeparator());
                    ToolStripButton btn = (text != "") ? new ToolStripButton(text, null, onClick) : new ToolStripButton("", image, onClick);
                    btn.ToolTipText = tooltip;
                    (control as ToolStrip).Items.Add(btn);
                    break;
                }
            }
        }

        /// <summary>Add a label to the property grid.</summary>
        public ToolStripLabel AddLabel(string text, Image image, string tooltip)
        {
            ToolStripLabel lbl = null;

            foreach (Control control in Controls)
            {
                if (control is ToolStrip)
                {
                    // Found toolstrip - add our stuff.
                    (control as ToolStrip).Items.Add(new ToolStripSeparator());
                    lbl = (text != "") ? new ToolStripLabel(text, null) : new ToolStripLabel("", image);
                    lbl.ToolTipText = tooltip;
                    (control as ToolStrip).Items.Add(lbl);
                    break;
                }
            }

            return lbl;
        }

        /// <summary>Moves the vertical splitter.</summary>
        public void MoveSplitter(int x)
        {
            // Go up in hierarchy until found real property grid type.
            var realType = GetType();
            while (realType != null && realType != typeof(PropertyGrid))
            {
                realType = realType.BaseType;
            }

            var gvf = realType.GetField("gridView", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            var gv = gvf.GetValue(this);

            var mtf = gv.GetType().GetMethod("MoveSplitterTo", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
            mtf.Invoke(gv, new object[] { (int)x });
        }

        /// <summary>Alter the bottom description area.</summary>
        public void ResizeDescriptionArea(int x)
        {
            var info = GetType().GetProperty("Controls");
            var collection = (Control.ControlCollection)info.GetValue(this, null);

            foreach (var control in collection)
            {
                var type = control.GetType();

                if ("DocComment" == type.Name)
                {
                    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    var field = type.BaseType.GetField("userSized", Flags);
                    field.SetValue(control, true);

                    info = type.GetProperty("Lines");
                    info.SetValue(control, x, null);

                    HelpVisible = true;
                    break;
                }
            }
        }
        
        /// <summary>Expand or collapse the group.</summary>
        public void ExpandGroup(string groupName, bool expand)
        {
            GridItem root = SelectedGridItem;

            // Get the parent
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            if (root != null)
            {
                foreach (GridItem g in root.GridItems)
                {
                    if (g.GridItemType == GridItemType.Category && g.Label.Trim() == groupName.Trim())
                    {
                        g.Expanded = expand;
                        break;
                    }
                }
            }
        }

        /// <summary>Show or hide a named property.</summary>
        public void ShowProperty(string which, bool visible)
        {
            // http://www.codeproject.com/Articles/152945/Enabling-disabling-properties-at-runtime-in-the-Pr
            // It is important to add the RefreshProperties attribute to the Country property. That will force the PropertyGrid control to refresh 
            //   all its properties every time the value of Country changes, reflecting the changes we made to the attributes of State.
            // In order for all this to work properly, it is important to statically define the ReadOnly attribute of every property of the 
            //   class to whatever value you want. If not, changing the attribute at runtime that way will wrongly modify the attributes of 
            //   every property of the class.

            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(SelectedObject);

            PropertyDescriptor descriptor = pdc[which];

            if (descriptor != null)
            {
                BrowsableAttribute attribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                FieldInfo fieldToChange = attribute.GetType().GetField("browsable", BindingFlags.NonPublic | BindingFlags.Instance);
                fieldToChange.SetValue(attribute, visible);

                Refresh();
            }
        }
    }
}