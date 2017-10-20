namespace Nebulator.UI
{
    partial class InfoDisplay
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnClear = new System.Windows.Forms.ToolStripButton();
            this.btnWrap = new System.Windows.Forms.ToolStripButton();
            this.txtInfo = new System.Windows.Forms.RichTextBox();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnClear,
            this.btnWrap});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(348, 25);
            this.toolStrip1.TabIndex = 17;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnClear
            // 
            this.btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnClear.Image = global::Nebulator.Properties.Resources.glyphicons_366_restart;
            this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(23, 22);
            this.btnClear.Text = "Clear";
            this.btnClear.ToolTipText = "Clear data";
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnWrap
            // 
            this.btnWrap.CheckOnClick = true;
            this.btnWrap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnWrap.Image = global::Nebulator.Properties.Resources.glyphicons_114_justify;
            this.btnWrap.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWrap.Name = "btnWrap";
            this.btnWrap.Size = new System.Drawing.Size(23, 22);
            this.btnWrap.Text = "toolStripButton1";
            this.btnWrap.ToolTipText = "Wrap text";
            this.btnWrap.Click += new System.EventHandler(this.btnWrap_Click);
            // 
            // txtInfo
            // 
            this.txtInfo.BackColor = System.Drawing.SystemColors.Window;
            this.txtInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInfo.Location = new System.Drawing.Point(0, 25);
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ReadOnly = true;
            this.txtInfo.Size = new System.Drawing.Size(348, 232);
            this.txtInfo.TabIndex = 19;
            this.txtInfo.Text = "";
            this.txtInfo.WordWrap = false;
            // 
            // InfoDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtInfo);
            this.Controls.Add(this.toolStrip1);
            this.Name = "InfoDisplay";
            this.Size = new System.Drawing.Size(348, 257);
            this.Load += new System.EventHandler(this.InfoDisplay_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.RichTextBox txtInfo;
        private System.Windows.Forms.ToolStripButton btnWrap;
    }
}
