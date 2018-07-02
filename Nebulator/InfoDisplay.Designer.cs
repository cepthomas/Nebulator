namespace Nebulator
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
            this.txtView = new System.Windows.Forms.RichTextBox();
            this.btnClear = new System.Windows.Forms.ToolStripButton();
            this.btnMonIn = new System.Windows.Forms.ToolStripButton();
            this.btnMonOut = new System.Windows.Forms.ToolStripButton();
            this.btnWrap = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnClear,
            this.btnMonIn,
            this.btnMonOut,
            this.btnWrap});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(516, 25);
            this.toolStrip1.TabIndex = 16;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // txtView
            // 
            this.txtView.BackColor = System.Drawing.SystemColors.Window;
            this.txtView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtView.Location = new System.Drawing.Point(0, 25);
            this.txtView.Name = "txtView";
            this.txtView.ReadOnly = true;
            this.txtView.Size = new System.Drawing.Size(516, 367);
            this.txtView.TabIndex = 17;
            this.txtView.Text = "";
            this.txtView.WordWrap = false;
            // 
            // btnClear
            // 
            this.btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnClear.Image = global::Nebulator.Properties.Resources.glyphicons_551_erase;
            this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(23, 22);
            this.btnClear.Text = "Clear";
            this.btnClear.ToolTipText = "Clear monitor data";
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // btnMonIn
            // 
            this.btnMonIn.CheckOnClick = true;
            this.btnMonIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMonIn.Image = global::Nebulator.Properties.Resources.glyphicons_213_arrow_down;
            this.btnMonIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMonIn.Name = "btnMonIn";
            this.btnMonIn.Size = new System.Drawing.Size(23, 22);
            this.btnMonIn.Text = "toolStripButton1";
            this.btnMonIn.ToolTipText = "Monitor messages in";
            this.btnMonIn.Click += new System.EventHandler(this.BtnMonIn_Click);
            // 
            // btnMonOut
            // 
            this.btnMonOut.CheckOnClick = true;
            this.btnMonOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMonOut.Image = global::Nebulator.Properties.Resources.glyphicons_214_arrow_up;
            this.btnMonOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMonOut.Name = "btnMonOut";
            this.btnMonOut.Size = new System.Drawing.Size(23, 22);
            this.btnMonOut.Text = "toolStripButton1";
            this.btnMonOut.ToolTipText = "Monitor messages out";
            this.btnMonOut.Click += new System.EventHandler(this.BtnMonOut_Click);
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
            this.btnWrap.Click += new System.EventHandler(this.Wrap_Click);
            // 
            // InfoDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtView);
            this.Controls.Add(this.toolStrip1);
            this.Name = "InfoDisplay";
            this.Size = new System.Drawing.Size(516, 392);
            this.Load += new System.EventHandler(this.InfoDisplay_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.RichTextBox txtView;
        private System.Windows.Forms.ToolStripButton btnMonIn;
        private System.Windows.Forms.ToolStripButton btnMonOut;
        private System.Windows.Forms.ToolStripButton btnWrap;
    }
}
