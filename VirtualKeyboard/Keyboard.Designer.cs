namespace Nebulator.VirtualKeyboard
{
    partial class VKeyboard
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
            this.SuspendLayout();
            // 
            // VKeyboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(773, 173);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "VKeyboard";
            this.Text = "VirtualKeyboard";
            this.Load += new System.EventHandler(this.Keyboard_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Keyboard_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Keyboard_KeyUp);
            this.Resize += new System.EventHandler(this.Keyboard_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}