namespace Nebulator.UI
{
    partial class ChannelControl_XXX
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
            this.components = new System.ComponentModel.Container();
            this.chkMute = new System.Windows.Forms.CheckBox();
            this.chkSolo = new System.Windows.Forms.CheckBox();
            this.sldVolume = new NBagOfUis.Slider();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            // 
            // chkMute
            // 
            this.chkMute.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkMute.BackColor = System.Drawing.SystemColors.ControlDark;
            this.chkMute.FlatAppearance.BorderSize = 0;
            this.chkMute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkMute.Location = new System.Drawing.Point(0, 0);
            this.chkMute.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkMute.Name = "chkMute";
            this.chkMute.Size = new System.Drawing.Size(12, 52);
            this.chkMute.TabIndex = 1;
            this.chkMute.Text = "M";
            this.toolTip.SetToolTip(this.chkMute, "Mute channel");
            this.chkMute.UseVisualStyleBackColor = false;
            this.chkMute.Click += new System.EventHandler(this.Check_Click);
            // 
            // chkSolo
            // 
            this.chkSolo.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkSolo.BackColor = System.Drawing.SystemColors.ControlDark;
            this.chkSolo.FlatAppearance.BorderSize = 0;
            this.chkSolo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkSolo.Location = new System.Drawing.Point(100, 0);
            this.chkSolo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkSolo.Name = "chkSolo";
            this.chkSolo.Size = new System.Drawing.Size(12, 52);
            this.chkSolo.TabIndex = 4;
            this.chkSolo.Text = "S";
            this.toolTip.SetToolTip(this.chkSolo, "Solo channel");
            this.chkSolo.UseVisualStyleBackColor = false;
            this.chkSolo.Click += new System.EventHandler(this.Check_Click);
            // 
            // sldVolume
            // 
            this.sldVolume.DrawColor = System.Drawing.Color.Orange;
            this.sldVolume.Label = "";
            this.sldVolume.Location = new System.Drawing.Point(12, 0);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.05D;
            this.sldVolume.Size = new System.Drawing.Size(88, 52);
            this.sldVolume.TabIndex = 5;
            this.toolTip.SetToolTip(this.sldVolume, "Channel volume");
            this.sldVolume.Value = 1D;
            this.sldVolume.ValueChanged += new System.EventHandler(this.VolChannel_ValueChanged);
            // 
            // ChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.chkSolo);
            this.Controls.Add(this.chkMute);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "ChannelControl";
            this.Size = new System.Drawing.Size(112, 52);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.CheckBox chkMute;
        private System.Windows.Forms.CheckBox chkSolo;
        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.ToolTip toolTip;
    }
}
