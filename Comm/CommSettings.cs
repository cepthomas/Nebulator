using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Newtonsoft.Json;
using Nebulator.Common;


namespace Nebulator.Comm
{
    [Serializable]
    public class CommSettings
    {
        #region Persisted editable properties
        [DisplayName("Input Device"), Description("Your choice of input."), Browsable(true)]
        [Editor(typeof(CommSelector), typeof(UITypeEditor))]
        public string InputDevice { get; set; } = Utils.UNKNOWN_STRING;

        [DisplayName("Output Device"), Description("Your choice of output."), Browsable(true)]
        [Editor(typeof(CommSelector), typeof(UITypeEditor))]
        public string OutputDevice { get; set; } = Utils.UNKNOWN_STRING;
        #endregion

        #region Persisted non-editable properties
        [Browsable(false)]
        public FormInfo PianoFormInfo { get; set; } = new FormInfo() { Height = 100, Width = 1000, Visible = true };

        [Browsable(false)]
        public bool MonitorInput { get; set; } = false;

        [Browsable(false)]
        public bool MonitorOutput { get; set; } = false;
        #endregion

        #region Fields
        /// <summary>The file name.</summary>
        string _fn = Utils.UNKNOWN_STRING;
        #endregion

        /// <summary>Current global user settings.</summary>
        public static CommSettings TheSettings { get; set; } = new CommSettings();

        /// <summary>Default constructor.</summary>
        public CommSettings()
        {
        }

        #region Persistence
        /// <summary>Save object to file.</summary>
        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_fn, json);
        }

        /// <summary>Create object from file.</summary>
        public static void Load(string appDir)
        {
            TheSettings = null;
            string fn = Path.Combine(appDir, "protocol.json");

            try
            {
                string json = File.ReadAllText(fn);
                TheSettings = JsonConvert.DeserializeObject<CommSettings>(json);
                TheSettings._fn = fn;
            }
            catch (Exception)
            {
                // Doesn't exist, create a new one.
                TheSettings = new CommSettings
                {
                    _fn = fn
                };
            }
        }
        #endregion
    }

    /// <summary
    /// >Plugin to property grid.
    /// </summary>
    public class CommSelector : UITypeEditor
    {
        IWindowsFormsEditorService _service = null;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            //Simple really, I just used the Instance property on the context parameter.
            //If this isn't null I can get access to my object being edited and hence my own business object model.

            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            ListBox lb = new ListBox() { SelectionMode = SelectionMode.One };
            lb.Click += (object sender, EventArgs e) => { _service.CloseDropDown(); };



            //// Fill the list box.
            //if (Options.ContainsKey(context.PropertyDescriptor.Name))
            //{
            //    Options[context.PropertyDescriptor.Name].ForEach(o =>
            //    {
            //        int i = lb.Items.Add(o);
            //        lb.SelectedIndex = o == value.ToString() ? i : lb.SelectedIndex;
            //    });
            //}
            //else
            //{
            //    throw new Exception($"Invalid value name: {context.PropertyDescriptor.Name}");
            //}

            // Alternatively you can ask the host for the options:
            //PropertyGridEx pgex = _service.GetType().GetProperty("Parent").GetValue(_service, null) as PropertyGridEx;
            //List<string> listvals = new List<string>();
            //pgex.RaisePropertyGridExEvent(context.PropertyDescriptor.Name, listvals);

            _service.DropDownControl(lb);
            return lb.SelectedItem == null ? "" : lb.SelectedItem.ToString();
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }

}
