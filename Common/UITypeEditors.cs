using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Reflection;
using System.Drawing;
using System.Drawing.Design;


namespace Nebulator.Common
{
    /// <summary
    /// >Plugin to property grid.
    /// </summary>
    public class ListSelector : UITypeEditor
    {
        /// <summary>The owner supplies the listbox contents using this. A bit kludgy...</summary>
        public static Dictionary<string, List<string>> Options { get; set; } = new Dictionary<string, List<string>>();

        IWindowsFormsEditorService _service = null;

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            ListBox lb = new ListBox() { SelectionMode = SelectionMode.One };
            lb.Click += (object sender, EventArgs e) => { _service.CloseDropDown(); };

            // Fill the list box.
            if(Options.ContainsKey(context.PropertyDescriptor.Name))
            {
                Options[context.PropertyDescriptor.Name].ForEach(o =>
                {
                    int i = lb.Items.Add(o);
                    lb.SelectedIndex = o == value.ToString() ? i : lb.SelectedIndex;
                });
            }
            else
            {
                throw new Exception($"Invalid value name: {context.PropertyDescriptor.Name}");
            }

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

    /// <summary>
    /// Plug in to property grid. String list is edited as a newline delimited string.
    /// </summary>
    public class StringListEditor : UITypeEditor
    {
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            if (value != null && value is List<string>)
            {
                List<string> vals = value as List<string>;

                TextBox tbox = new TextBox()
                {
                    Multiline = true,
                    Height = 100,
                    ScrollBars = ScrollBars.Vertical,
                    //AcceptsReturn = true
                    Text = string.Join(Environment.NewLine, vals)
                };

                tbox.Select(0, 0);

                tbox.KeyDown += (object sender, KeyEventArgs e) =>
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Enter:
                            tbox.AppendText(Environment.NewLine);
                            e.Handled = true;
                            break;

                        default:
                            // Don't care;
                            break;
                    }
                };

                editorService.DropDownControl(tbox);

                // Done.
                vals.Clear();
                tbox.Text.SplitByToken(Environment.NewLine).ForEach(s => vals.Add(s));

                value = vals;
            }

            return value.DeepClone(); // Forces prop grid to see the change.
        }

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }
}
