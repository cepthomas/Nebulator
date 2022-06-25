namespace Nebulator.App
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            Nebulator.Common.Time time1 = new Nebulator.Common.Time();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.sldVolume = new NBagOfUis.Slider();
            this.sldSpeed = new NBagOfUis.Slider();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.btnCompile = new System.Windows.Forms.Button();
            this.btnRewind = new System.Windows.Forms.Button();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMonIn = new System.Windows.Forms.ToolStripButton();
            this.btnMonOut = new System.Windows.Forms.ToolStripButton();
            this.btnKillComm = new System.Windows.Forms.ToolStripButton();
            this.btnClear = new System.Windows.Forms.ToolStripButton();
            this.btnWrap = new System.Windows.Forms.ToolStripButton();
            this.textViewer = new NBagOfUis.TextViewer();
            this.lblSolo = new System.Windows.Forms.Label();
            this.lblMute = new System.Windows.Forms.Label();
            this.timeMaster = new Nebulator.UI.TimeControl();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // sldVolume
            // 
            this.sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldVolume.DrawColor = System.Drawing.Color.Orange;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(267, 49);
            this.sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.sldVolume.Maximum = 1D;
            this.sldVolume.Minimum = 0D;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldVolume.Resolution = 0.05D;
            this.sldVolume.Size = new System.Drawing.Size(88, 52);
            this.sldVolume.TabIndex = 36;
            this.toolTip.SetToolTip(this.sldVolume, "Master volume");
            this.sldVolume.Value = 1D;
            this.sldVolume.ValueChanged += new System.EventHandler(this.Volume_ValueChanged);
            // 
            // sldSpeed
            // 
            this.sldSpeed.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sldSpeed.DrawColor = System.Drawing.Color.LightGray;
            this.sldSpeed.Label = "bpm";
            this.sldSpeed.Location = new System.Drawing.Point(170, 49);
            this.sldSpeed.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.sldSpeed.Maximum = 200D;
            this.sldSpeed.Minimum = 30D;
            this.sldSpeed.Name = "sldSpeed";
            this.sldSpeed.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.sldSpeed.Resolution = 5D;
            this.sldSpeed.Size = new System.Drawing.Size(88, 52);
            this.sldSpeed.TabIndex = 33;
            this.toolTip.SetToolTip(this.sldSpeed, "Speed in BPM");
            this.sldSpeed.Value = 100D;
            this.sldSpeed.ValueChanged += new System.EventHandler(this.Speed_ValueChanged);
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
            // btnCompile
            // 
            this.btnCompile.FlatAppearance.BorderSize = 0;
            this.btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompile.Image = global::App.Properties.Resources.glyphicons_366_restart;
            this.btnCompile.Location = new System.Drawing.Point(116, 49);
            this.btnCompile.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(45, 49);
            this.btnCompile.TabIndex = 38;
            this.toolTip.SetToolTip(this.btnCompile, "Compile script file - lit indicates file changed externally");
            this.btnCompile.UseVisualStyleBackColor = false;
            this.btnCompile.Click += new System.EventHandler(this.Compile_Click);
            // 
            // btnRewind
            // 
            this.btnRewind.FlatAppearance.BorderSize = 0;
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::App.Properties.Resources.glyphicons_173_rewind;
            this.btnRewind.Location = new System.Drawing.Point(13, 49);
            this.btnRewind.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(45, 49);
            this.btnRewind.TabIndex = 31;
            this.toolTip.SetToolTip(this.btnRewind, "Reset to start");
            this.btnRewind.UseVisualStyleBackColor = false;
            this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.BackColor = System.Drawing.SystemColors.Control;
            this.chkPlay.FlatAppearance.BorderSize = 0;
            this.chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::App.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(65, 49);
            this.chkPlay.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.chkPlay.MaximumSize = new System.Drawing.Size(43, 49);
            this.chkPlay.MinimumSize = new System.Drawing.Size(43, 49);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(43, 49);
            this.chkPlay.TabIndex = 35;
            this.toolTip.SetToolTip(this.chkPlay, "Play project");
            this.chkPlay.UseVisualStyleBackColor = false;
            this.chkPlay.Click += new System.EventHandler(this.Play_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileDropDownButton,
            this.btnMonIn,
            this.btnMonOut,
            this.btnKillComm,
            this.btnClear,
            this.btnWrap});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(976, 27);
            this.toolStrip1.TabIndex = 39;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // fileDropDownButton
            // 
            this.fileDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.fileDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.exportMidiToolStripMenuItem,
            this.viewLogToolStripMenuItem,
            this.aboutToolStripMenuItem,
            this.settingsToolStripMenuItem});
            this.fileDropDownButton.Image = global::App.Properties.Resources.glyphicons_37_file;
            this.fileDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fileDropDownButton.Name = "fileDropDownButton";
            this.fileDropDownButton.Size = new System.Drawing.Size(34, 24);
            this.fileDropDownButton.Text = "fileDropDownButton";
            this.fileDropDownButton.ToolTipText = "File operations";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.openToolStripMenuItem.Text = "Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.Open_Click);
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.recentToolStripMenuItem.Text = "Recent";
            // 
            // exportMidiToolStripMenuItem
            // 
            this.exportMidiToolStripMenuItem.Name = "exportMidiToolStripMenuItem";
            this.exportMidiToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.exportMidiToolStripMenuItem.Text = "Export Midi";
            this.exportMidiToolStripMenuItem.Click += new System.EventHandler(this.ExportMidi_Click);
            // 
            // viewLogToolStripMenuItem
            // 
            this.viewLogToolStripMenuItem.Name = "viewLogToolStripMenuItem";
            this.viewLogToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.viewLogToolStripMenuItem.Text = "Show Log...";
            this.viewLogToolStripMenuItem.ToolTipText = "Let\'s have a look at what happened";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Image = global::App.Properties.Resources.glyphicons_195_question_sign;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.About_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Image = global::App.Properties.Resources.glyphicons_137_cogwheel;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(224, 26);
            this.settingsToolStripMenuItem.Text = "Settings...";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.UserSettings_Click);
            // 
            // btnMonIn
            // 
            this.btnMonIn.CheckOnClick = true;
            this.btnMonIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMonIn.Image = global::App.Properties.Resources.glyphicons_213_arrow_down;
            this.btnMonIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMonIn.Name = "btnMonIn";
            this.btnMonIn.Size = new System.Drawing.Size(29, 24);
            this.btnMonIn.Text = "toolStripButton1";
            this.btnMonIn.ToolTipText = "Monitor messages in";
            this.btnMonIn.Click += new System.EventHandler(this.Mon_Click);
            // 
            // btnMonOut
            // 
            this.btnMonOut.CheckOnClick = true;
            this.btnMonOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnMonOut.Image = global::App.Properties.Resources.glyphicons_214_arrow_up;
            this.btnMonOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnMonOut.Name = "btnMonOut";
            this.btnMonOut.Size = new System.Drawing.Size(29, 24);
            this.btnMonOut.Text = "toolStripButton1";
            this.btnMonOut.ToolTipText = "Monitor messages out";
            this.btnMonOut.Click += new System.EventHandler(this.Mon_Click);
            // 
            // btnKillComm
            // 
            this.btnKillComm.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnKillComm.Image = global::App.Properties.Resources.glyphicons_206_electricity;
            this.btnKillComm.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnKillComm.Name = "btnKillComm";
            this.btnKillComm.Size = new System.Drawing.Size(29, 24);
            this.btnKillComm.Text = "toolStripButton1";
            this.btnKillComm.ToolTipText = "Instant stop all devices";
            // 
            // btnClear
            // 
            this.btnClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnClear.Image = global::App.Properties.Resources.glyphicons_551_erase;
            this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(29, 24);
            this.btnClear.Text = "toolStripButton1";
            this.btnClear.ToolTipText = "Clear the display";
            // 
            // btnWrap
            // 
            this.btnWrap.CheckOnClick = true;
            this.btnWrap.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnWrap.Image = global::App.Properties.Resources.glyphicons_114_justify;
            this.btnWrap.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWrap.Name = "btnWrap";
            this.btnWrap.Size = new System.Drawing.Size(29, 24);
            this.btnWrap.Text = "toolStripButton1";
            this.btnWrap.ToolTipText = "Enable wordwrap";
            // 
            // textViewer
            // 
            this.textViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textViewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textViewer.Location = new System.Drawing.Point(13, 334);
            this.textViewer.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textViewer.MaxText = 5000;
            this.textViewer.Name = "textViewer";
            this.textViewer.Size = new System.Drawing.Size(952, 493);
            this.textViewer.TabIndex = 41;
            this.textViewer.WordWrap = true;
            // 
            // lblSolo
            // 
            this.lblSolo.AutoSize = true;
            this.lblSolo.Location = new System.Drawing.Point(843, 49);
            this.lblSolo.Name = "lblSolo";
            this.lblSolo.Size = new System.Drawing.Size(39, 20);
            this.lblSolo.TabIndex = 42;
            this.lblSolo.Text = "Solo";
            // 
            // lblMute
            // 
            this.lblMute.AutoSize = true;
            this.lblMute.Location = new System.Drawing.Point(843, 80);
            this.lblMute.Name = "lblMute";
            this.lblMute.Size = new System.Drawing.Size(43, 20);
            this.lblMute.TabIndex = 43;
            this.lblMute.Text = "Mute";
            // 
            // timeMaster
            // 
            this.timeMaster.ControlColor = System.Drawing.Color.Orange;
            time1.Beat = 0;
            time1.Subdiv = 0;
            this.timeMaster.CurrentTime = time1;
            this.timeMaster.Font = new System.Drawing.Font("Consolas", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.timeMaster.Location = new System.Drawing.Point(371, 49);
            this.timeMaster.Margin = new System.Windows.Forms.Padding(12, 14, 12, 14);
            this.timeMaster.MaxBeat = 0;
            this.timeMaster.Name = "timeMaster";
            this.timeMaster.ShowProgress = true;
            this.timeMaster.Size = new System.Drawing.Size(233, 52);
            this.timeMaster.TabIndex = 37;
            this.timeMaster.ValueChanged += new System.EventHandler(this.Time_ValueChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(976, 838);
            this.Controls.Add(this.lblMute);
            this.Controls.Add(this.lblSolo);
            this.Controls.Add(this.textViewer);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnCompile);
            this.Controls.Add(this.timeMaster);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.sldSpeed);
            this.Controls.Add(this.btnRewind);
            this.Controls.Add(this.chkPlay);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "Nebulator";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private NBagOfUis.Slider sldSpeed;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.ToolTip toolTip;
        private UI.TimeControl timeMaster;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnMonIn;
        private System.Windows.Forms.ToolStripButton btnMonOut;
        private System.Windows.Forms.ToolStripDropDownButton fileDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportMidiToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnKillComm;
        private System.Windows.Forms.ToolStripMenuItem viewLogToolStripMenuItem;
        private NBagOfUis.TextViewer textViewer;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.ToolStripButton btnWrap;
        private System.Windows.Forms.Label lblSolo;
        private System.Windows.Forms.Label lblMute;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
    }
}

