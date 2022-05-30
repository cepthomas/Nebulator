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
//TODOX            this.vkey = new VirtualKeyboard();
            this.SuspendLayout();
            // 
            // vkey
            // 
            //this.vkey.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.vkey.Location = new System.Drawing.Point(0, 0);
            //this.vkey.Margin = new System.Windows.Forms.Padding(5, 8, 5, 8);
            //this.vkey.Name = "vkey";
            //this.vkey.ShowNoteNames = false;
            //this.vkey.Size = new System.Drawing.Size(880, 198);
            //this.vkey.TabIndex = 0;
            // 
            // Keyboard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(880, 198);
//TODOX            this.Controls.Add(this.vkey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Keyboard";
            this.Text = "Keyboard";
            this.ResumeLayout(false);

        }

        #endregion

//TODOX        private VirtualKeyboard vkey;
    }
}