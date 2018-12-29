namespace Nebulator
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
            this.btnCompile = new System.Windows.Forms.Button();
            this.sldVolume = new Nebulator.Controls.Slider();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.potSpeed = new Nebulator.Controls.Pot();
            this.btnRewind = new System.Windows.Forms.Button();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnMonIn = new System.Windows.Forms.ToolStripButton();
            this.btnMonOut = new System.Windows.Forms.ToolStripButton();
            this.btnKillComm = new System.Windows.Forms.ToolStripButton();
            this.btnSettings = new System.Windows.Forms.ToolStripButton();
            this.btnAbout = new System.Windows.Forms.ToolStripButton();
            this.levers = new Nebulator.Levers();
            this.timeMaster = new Nebulator.Controls.TimeControl();
            this.textViewer = new Nebulator.Controls.TextViewer();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCompile
            // 
            this.btnCompile.FlatAppearance.BorderSize = 0;
            this.btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompile.Image = global::Nebulator.Properties.Resources.glyphicons_366_restart;
            this.btnCompile.Location = new System.Drawing.Point(78, 32);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(34, 32);
            this.btnCompile.TabIndex = 38;
            this.toolTip.SetToolTip(this.btnCompile, "Compile script file - lit indicates file changed externally");
            this.btnCompile.UseVisualStyleBackColor = false;
            this.btnCompile.Click += new System.EventHandler(this.Compile_Click);
            // 
            // sldVolume
            // 
            this.sldVolume.ControlColor = System.Drawing.Color.Orange;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(158, 32);
            this.sldVolume.Maximum = 1.0;
            this.sldVolume.Minimum = 0;
            this.sldVolume.Name = "sldVolume";
            this.sldVolume.ResetValue = 0;
            this.sldVolume.Size = new System.Drawing.Size(66, 34);
            this.sldVolume.TabIndex = 36;
            this.toolTip.SetToolTip(this.sldVolume, "Master volume");
            this.sldVolume.Value = 90;
            this.sldVolume.ValueChanged += new System.EventHandler(this.Volume_ValueChanged);
            // 
            // chkPlay
            // 
            this.chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPlay.BackColor = System.Drawing.SystemColors.Control;
            this.chkPlay.FlatAppearance.BorderSize = 0;
            this.chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkPlay.Image = global::Nebulator.Properties.Resources.glyphicons_174_play;
            this.chkPlay.Location = new System.Drawing.Point(49, 32);
            this.chkPlay.MaximumSize = new System.Drawing.Size(32, 32);
            this.chkPlay.MinimumSize = new System.Drawing.Size(32, 32);
            this.chkPlay.Name = "chkPlay";
            this.chkPlay.Size = new System.Drawing.Size(32, 32);
            this.chkPlay.TabIndex = 35;
            this.toolTip.SetToolTip(this.chkPlay, "Play project");
            this.chkPlay.UseVisualStyleBackColor = false;
            this.chkPlay.Click += new System.EventHandler(this.Play_Click);
            // 
            // potSpeed
            // 
            this.potSpeed.ControlColor = System.Drawing.Color.Black;
            this.potSpeed.DecPlaces = 0;
            this.potSpeed.Location = new System.Drawing.Point(117, 32);
            this.potSpeed.Maximum = 200;
            this.potSpeed.Minimum = 30;
            this.potSpeed.Name = "potSpeed";
            this.potSpeed.Size = new System.Drawing.Size(32, 32);
            this.potSpeed.TabIndex = 33;
            this.toolTip.SetToolTip(this.potSpeed, "Speed in Ticks per minute (sorta BPM)");
            this.potSpeed.Value = 100D;
            this.potSpeed.ValueChanged += new System.EventHandler(this.Speed_ValueChanged);
            // 
            // btnRewind
            // 
            this.btnRewind.FlatAppearance.BorderSize = 0;
            this.btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRewind.Image = global::Nebulator.Properties.Resources.glyphicons_172_fast_backward;
            this.btnRewind.Location = new System.Drawing.Point(10, 32);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(34, 32);
            this.btnRewind.TabIndex = 31;
            this.toolTip.SetToolTip(this.btnRewind, "Reset to start");
            this.btnRewind.UseVisualStyleBackColor = false;
            this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);
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
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileDropDownButton,
            this.btnMonIn,
            this.btnMonOut,
            this.btnKillComm,
            this.btnSettings,
            this.btnAbout});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(732, 25);
            this.toolStrip1.TabIndex = 39;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // fileDropDownButton
            // 
            this.fileDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.fileDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.importMidiToolStripMenuItem,
            this.exportMidiToolStripMenuItem,
            this.viewLogToolStripMenuItem});
            this.fileDropDownButton.Image = global::Nebulator.Properties.Resources.glyphicons_37_file;
            this.fileDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.fileDropDownButton.Name = "fileDropDownButton";
            this.fileDropDownButton.Size = new System.Drawing.Size(29, 22);
            this.fileDropDownButton.Text = "fileDropDownButton";
            this.fileDropDownButton.ToolTipText = "File operations";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.Open_Click);
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.recentToolStripMenuItem.Text = "Recent";
            // 
            // importMidiToolStripMenuItem
            // 
            this.importMidiToolStripMenuItem.Name = "importMidiToolStripMenuItem";
            this.importMidiToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.importMidiToolStripMenuItem.Text = "Import Midi or Style";
            this.importMidiToolStripMenuItem.Click += new System.EventHandler(this.ImportMidi_Click);
            // 
            // exportMidiToolStripMenuItem
            // 
            this.exportMidiToolStripMenuItem.Name = "exportMidiToolStripMenuItem";
            this.exportMidiToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportMidiToolStripMenuItem.Text = "Export Midi";
            this.exportMidiToolStripMenuItem.Click += new System.EventHandler(this.ExportMidi_Click);
            // 
            // viewLogToolStripMenuItem
            // 
            this.viewLogToolStripMenuItem.Name = "viewLogToolStripMenuItem";
            this.viewLogToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.viewLogToolStripMenuItem.Text = "Show Log";
            this.viewLogToolStripMenuItem.ToolTipText = "Let\'s have a look at what happened";
            this.viewLogToolStripMenuItem.Click += new System.EventHandler(this.LogShow_Click);
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
            this.btnMonIn.Click += new System.EventHandler(this.Mon_Click);
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
            this.btnMonOut.Click += new System.EventHandler(this.Mon_Click);
            // 
            // btnKillComm
            // 
            this.btnKillComm.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnKillComm.Image = global::Nebulator.Properties.Resources.glyphicons_206_electricity;
            this.btnKillComm.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnKillComm.Name = "btnKillComm";
            this.btnKillComm.Size = new System.Drawing.Size(23, 22);
            this.btnKillComm.Text = "toolStripButton1";
            this.btnKillComm.ToolTipText = "Instant stop all devices";
            this.btnKillComm.Click += new System.EventHandler(this.Kill_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSettings.Image = global::Nebulator.Properties.Resources.glyphicons_137_cogwheel;
            this.btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(23, 22);
            this.btnSettings.Text = "toolStripButton1";
            this.btnSettings.ToolTipText = "Settings";
            this.btnSettings.Click += new System.EventHandler(this.UserSettings_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnAbout.Image = global::Nebulator.Properties.Resources.glyphicons_195_question_sign;
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(23, 22);
            this.btnAbout.Text = "toolStripButton1";
            this.btnAbout.ToolTipText = "General info and a list of your devices";
            this.btnAbout.Click += new System.EventHandler(this.About_Click);
            // 
            // levers
            // 
            this.levers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.levers.BackColor = System.Drawing.Color.AliceBlue;
            this.levers.Location = new System.Drawing.Point(10, 78);
            this.levers.Name = "levers";
            this.levers.Size = new System.Drawing.Size(714, 42);
            this.levers.TabIndex = 0;
            // 
            // timeMaster
            // 
            this.timeMaster.ControlColor = System.Drawing.Color.Orange;
            time1.Tick = 0;
            time1.Tock = 0;
            this.timeMaster.CurrentTime = time1;
            this.timeMaster.Font = new System.Drawing.Font("Consolas", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeMaster.Location = new System.Drawing.Point(231, 32);
            this.timeMaster.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            this.timeMaster.MaxTick = 0;
            this.timeMaster.Name = "timeMaster";
            this.timeMaster.ShowProgress = true;
            this.timeMaster.Size = new System.Drawing.Size(175, 34);
            this.timeMaster.TabIndex = 37;
            this.timeMaster.ValueChanged += new System.EventHandler(this.Time_ValueChanged);
            // 
            // textViewer
            // 
            this.textViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textViewer.Location = new System.Drawing.Point(10, 126);
            this.textViewer.Name = "textViewer";
            this.textViewer.Size = new System.Drawing.Size(714, 412);
            this.textViewer.TabIndex = 41;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(732, 544);
            this.Controls.Add(this.textViewer);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.levers);
            this.Controls.Add(this.btnCompile);
            this.Controls.Add(this.timeMaster);
            this.Controls.Add(this.sldVolume);
            this.Controls.Add(this.potSpeed);
            this.Controls.Add(this.btnRewind);
            this.Controls.Add(this.chkPlay);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Nebulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Controls.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private Controls.Pot potSpeed;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.ToolTip toolTip;
        private Levers levers;
        private Controls.TimeControl timeMaster;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnMonIn;
        private System.Windows.Forms.ToolStripButton btnMonOut;
        private System.Windows.Forms.ToolStripDropDownButton fileDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importMidiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportMidiToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnKillComm;
        private System.Windows.Forms.ToolStripButton btnSettings;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.ToolStripMenuItem viewLogToolStripMenuItem;
        private Controls.TextViewer textViewer;
    }
}

