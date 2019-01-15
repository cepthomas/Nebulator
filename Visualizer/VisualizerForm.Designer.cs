namespace Nebulator.Visualizer
{
    partial class VisualizerForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VisualizerForm));
            this.skControl = new SkiaSharp.Views.Desktop.SKControl();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolStripMain = new System.Windows.Forms.ToolStrip();
            this.btnGo = new System.Windows.Forms.ToolStripButton();
            this.btnPlus = new System.Windows.Forms.ToolStripButton();
            this.btnMinus = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.skControlInfo = new SkiaSharp.Views.Desktop.SKControl();
            this.toolStripMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // skControl
            // 
            this.skControl.BackColor = System.Drawing.Color.White;
            this.skControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skControl.Location = new System.Drawing.Point(0, 0);
            this.skControl.Name = "skControl";
            this.skControl.Size = new System.Drawing.Size(722, 540);
            this.skControl.TabIndex = 0;
            this.skControl.Text = "skControl1";
            // 
            // toolTip
            // 
            this.toolTip.AutomaticDelay = 0;
            this.toolTip.AutoPopDelay = 0;
            this.toolTip.InitialDelay = 300;
            this.toolTip.ReshowDelay = 0;
            this.toolTip.UseAnimation = false;
            this.toolTip.UseFading = false;
            // 
            // toolStripMain
            // 
            this.toolStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnGo,
            this.btnPlus,
            this.btnMinus});
            this.toolStripMain.Location = new System.Drawing.Point(0, 0);
            this.toolStripMain.Name = "toolStripMain";
            this.toolStripMain.Size = new System.Drawing.Size(942, 25);
            this.toolStripMain.TabIndex = 1;
            this.toolStripMain.Text = "toolStrip1";
            // 
            // btnGo
            // 
            this.btnGo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnGo.Image = ((System.Drawing.Image)(resources.GetObject("btnGo.Image")));
            this.btnGo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(26, 22);
            this.btnGo.Text = "Go";
            // 
            // btnPlus
            // 
            this.btnPlus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnPlus.Image = ((System.Drawing.Image)(resources.GetObject("btnPlus.Image")));
            this.btnPlus.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPlus.Name = "btnPlus";
            this.btnPlus.Size = new System.Drawing.Size(23, 22);
            this.btnPlus.Text = "+";
            this.btnPlus.Click += new System.EventHandler(this.BtnPlus_Click);
            // 
            // btnMinus
            // 
            this.btnMinus.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnMinus.Image = ((System.Drawing.Image)(resources.GetObject("btnMinus.Image")));
            this.btnMinus.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMinus.Name = "btnMinus";
            this.btnMinus.Size = new System.Drawing.Size(23, 22);
            this.btnMinus.Text = "-";
            this.btnMinus.Click += new System.EventHandler(this.BtnMinus_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.skControlInfo);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.skControl);
            this.splitContainer1.Size = new System.Drawing.Size(942, 540);
            this.splitContainer1.SplitterDistance = 216;
            this.splitContainer1.TabIndex = 2;
            // 
            // skControlInfo
            // 
            this.skControlInfo.BackColor = System.Drawing.Color.White;
            this.skControlInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.skControlInfo.Location = new System.Drawing.Point(0, 0);
            this.skControlInfo.Name = "skControlInfo";
            this.skControlInfo.Size = new System.Drawing.Size(216, 540);
            this.skControlInfo.TabIndex = 0;
            this.skControlInfo.Text = "skControl1";
            this.skControlInfo.PaintSurface += new System.EventHandler<SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs>(this.SkControlInfo_PaintSurface);
            // 
            // VisualizerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(942, 565);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStripMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Location = new System.Drawing.Point(50, 50);
            this.Name = "VisualizerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Visualizer";
            this.Load += new System.EventHandler(this.VisualizerForm_Load);
            this.toolStripMain.ResumeLayout(false);
            this.toolStripMain.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private SkiaSharp.Views.Desktop.SKControl skControl;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStrip toolStripMain;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripButton btnGo;
        private SkiaSharp.Views.Desktop.SKControl skControlInfo;
        private System.Windows.Forms.ToolStripButton btnPlus;
        private System.Windows.Forms.ToolStripButton btnMinus;
    }
}

