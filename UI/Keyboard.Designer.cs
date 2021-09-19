namespace Nebulator.UI
{
    partial class Keyboard
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
            this.vkey = new NBagOfTricks.UI.VirtualKeyboard();
            this.SuspendLayout();
            // 
            // vkey
            // 
            this.vkey.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vkey.Location = new System.Drawing.Point(0, 0);
            this.vkey.Name = "vkey";
            this.vkey.Size = new System.Drawing.Size(660, 129);
            this.vkey.TabIndex = 0;
            // 
            // Keyboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(660, 129);
            this.Controls.Add(this.vkey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "Keyboard";
            this.Text = "Keyboard";
            this.Load += new System.EventHandler(this.Keyboard_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private NBagOfTricks.UI.VirtualKeyboard vkey;
    }
}