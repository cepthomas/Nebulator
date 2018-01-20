using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;
using Nebulator.Common;


namespace Nebulator.Midi
{
    /// <summary>Plugin to property grid.</summary>
    public class MidiPortEditor : UITypeEditor
    {
        IWindowsFormsEditorService _service = null;

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            _service = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            string selval = null;

            ListBox lb = new ListBox() { SelectionMode = SelectionMode.One };
            lb.Click += ListBox_Click;

            // Fill the list box.
            switch (context.PropertyDescriptor.Name)
            {
                case "MidiIn":
                    foreach (string s in MidiInterface.TheInterface.MidiInputs)
                    {
                        int i = lb.Items.Add(s);
                        if(s == UserSettings.TheSettings.MidiIn)
                        {
                            lb.SelectedIndex = i;
                        }
                    }
                    break;

                case "MidiOut":
                    foreach (string s in MidiInterface.TheInterface.MidiOutputs)
                    {
                        int i = lb.Items.Add(s);
                        if (s == UserSettings.TheSettings.MidiOut)
                        {
                            lb.SelectedIndex = i;
                        }
                    }
                    break;
            }

            _service.DropDownControl(lb);

            if (lb.SelectedItem != null)
            {
                selval = lb.SelectedItem.ToString();
            }

            return selval;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBox_Click(object sender, EventArgs e)
        {
            if (_service != null)
            {
                _service.CloseDropDown();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.DropDown;
        }
    }
}
