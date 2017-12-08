namespace Nebulator.Editor
{
    partial class NebEditor
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NebEditor));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonSplit = new System.Windows.Forms.ToolStripButton();
            this.lblInfo = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonSplit,
            this.lblInfo});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(251, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolStripButtonSplit
            // 
            this.toolStripButtonSplit.CheckOnClick = true;
            this.toolStripButtonSplit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonSplit.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSplit.Image")));
            this.toolStripButtonSplit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSplit.Name = "toolStripButtonSplit";
            this.toolStripButtonSplit.Size = new System.Drawing.Size(34, 22);
            this.toolStripButtonSplit.Text = "Split";
            this.toolStripButtonSplit.Click += new System.EventHandler(this.btnSplit_Click);
            // 
            // lblInfo
            // 
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(16, 22);
            this.lblInfo.Text = "!!!";
            // 
            // NebEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStrip);
            this.Name = "NebEditor";
            this.Size = new System.Drawing.Size(251, 266);
            this.Load += new System.EventHandler(this.NebEditor_Load);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonSplit;
        private System.Windows.Forms.ToolStripLabel lblInfo;
    }
}
