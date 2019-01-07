namespace Nebulator.Synth
{
    partial class Visualizer
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
            this.skControl = new SkiaSharp.Views.Desktop.SKControl();
            this.SuspendLayout();
            // 
            // skControl
            // 
            this.skControl.BackColor = System.Drawing.Color.White;
            this.skControl.Location = new System.Drawing.Point(39, 39);
            this.skControl.Name = "skControl";
            this.skControl.Size = new System.Drawing.Size(433, 332);
            this.skControl.TabIndex = 0;
            this.skControl.Text = "skControl1";
            this.skControl.PaintSurface += new System.EventHandler<SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs>(this.skControl_PaintSurface);
            // 
            // Visualizer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(502, 383);
            this.Controls.Add(this.skControl);
            this.Name = "Visualizer";
            this.Text = "Visualizer";
            this.Load += new System.EventHandler(this.Visualizer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private SkiaSharp.Views.Desktop.SKControl skControl;
    }
}