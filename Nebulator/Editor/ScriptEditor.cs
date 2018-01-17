using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nebulator.Editor
{
    public partial class ScriptEditor : Form
    {
        public ScriptEditor()
        {
            InitializeComponent();
        }

        private void ScriptEditor_Load(object sender, EventArgs e)
        {
                
        }


        /// <summary>
        /// Tester for chart/grid.
        /// </summary>
        public void TestGrid()
        {
            Grid grid = new Grid() { Dock = DockStyle.Fill };
            splitContainer1.Panel1.Controls.Add(grid);
            Random rand = new Random(111);
            grid.ToolTipEvent += ((s, e) => e.Text = "TT_" + rand.Next().ToString());
            grid.InitData();
            grid.Show();
        }
    }
}
