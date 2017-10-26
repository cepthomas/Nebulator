using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using System.Drawing.Design;


namespace Nebulator.Common
{
    /// <summary>
    /// Plug in to property grid.
    /// </summary>
    public class StringListEditor : UITypeEditor
    {
        TextBox _tbox = null;

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;

            if (value != null && value is List<string>)
            {
                List<string> vals = value as List<string>;
                //List<string> vals = value.ToString().SplitByTokens(Environment.NewLine);

                _tbox = new TextBox()
                {
                    Multiline = true,
                    Height = 100,
                    ScrollBars = ScrollBars.Vertical,
                    //AcceptsReturn = true
                    Text = string.Join(Environment.NewLine, vals)
                };

                _tbox.Select(0, 0);
                _tbox.KeyDown += Text_KeyDown;
                _tbox.Leave += Text_Leave;

                editorService.DropDownControl(_tbox);

                // Done.
                vals.Clear();

                foreach (string s in _tbox.Text.SplitByToken(Environment.NewLine))
                {
                    vals.Add(s);
                }

                value = vals;
            }

            return value.DeepClone(); // Forces prop grid to see the change.
        }

        /// <summary>
        /// Handle enter key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Text_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    _tbox.AppendText(Environment.NewLine);
                    e.Handled = true;
                    break;

                default:
                    // Don't care;
                    break;
            }
        }

        /// <summary>
        /// Save entered values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Text_Leave(object sender, EventArgs e)
        {
            // Done.
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
