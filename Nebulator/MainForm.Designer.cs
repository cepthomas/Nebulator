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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.killMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pianoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.chkSeq = new System.Windows.Forms.CheckBox();
            this.chkUi = new System.Windows.Forms.CheckBox();
            this.btnCompile = new System.Windows.Forms.Button();
            this.timeMaster = new Nebulator.Controls.TimeControl();
            this.sldVolume = new Nebulator.Controls.Slider();
            this.chkPlay = new System.Windows.Forms.CheckBox();
            this.potSpeed = new Nebulator.Controls.Pot();
            this.btnRewind = new System.Windows.Forms.Button();
            this.splitContainerControl = new System.Windows.Forms.SplitContainer();
            this.levers = new Nebulator.Levers();
            this.infoDisplay = new Nebulator.InfoDisplay();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl)).BeginInit();
            this.splitContainerControl.Panel1.SuspendLayout();
            this.splitContainerControl.Panel2.SuspendLayout();
            this.splitContainerControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(826, 24);
            this.menuStrip1.TabIndex = 15;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.recentToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.Open_Click);
            // 
            // recentToolStripMenuItem
            // 
            this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            this.recentToolStripMenuItem.Size = new System.Drawing.Size(110, 22);
            this.recentToolStripMenuItem.Text = "Recent";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.killMidiToolStripMenuItem,
            this.pianoToolStripMenuItem,
            this.logToolStripMenuItem,
            this.importMidiToolStripMenuItem,
            this.exportMidiToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // killMidiToolStripMenuItem
            // 
            this.killMidiToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_206_electricity;
            this.killMidiToolStripMenuItem.Name = "killMidiToolStripMenuItem";
            this.killMidiToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.killMidiToolStripMenuItem.Text = "Kill Midi";
            this.killMidiToolStripMenuItem.ToolTipText = "Instant stop all devices";
            this.killMidiToolStripMenuItem.Click += new System.EventHandler(this.Kill_Click);
            // 
            // pianoToolStripMenuItem
            // 
            this.pianoToolStripMenuItem.CheckOnClick = true;
            this.pianoToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_327_piano;
            this.pianoToolStripMenuItem.Name = "pianoToolStripMenuItem";
            this.pianoToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.pianoToolStripMenuItem.Text = "Piano";
            this.pianoToolStripMenuItem.Click += new System.EventHandler(this.Piano_Click);
            // 
            // logToolStripMenuItem
            // 
            this.logToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_331_blog;
            this.logToolStripMenuItem.Name = "logToolStripMenuItem";
            this.logToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.logToolStripMenuItem.Text = "View Log";
            this.logToolStripMenuItem.Click += new System.EventHandler(this.LogShow_Click);
            // 
            // importMidiToolStripMenuItem
            // 
            this.importMidiToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_359_file_import;
            this.importMidiToolStripMenuItem.Name = "importMidiToolStripMenuItem";
            this.importMidiToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.importMidiToolStripMenuItem.Text = "Import Style";
            this.importMidiToolStripMenuItem.Click += new System.EventHandler(this.ImportStyle_Click);
            // 
            // exportMidiToolStripMenuItem
            // 
            this.exportMidiToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_360_file_export;
            this.exportMidiToolStripMenuItem.Name = "exportMidiToolStripMenuItem";
            this.exportMidiToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.exportMidiToolStripMenuItem.Text = "Export Midi";
            this.exportMidiToolStripMenuItem.Click += new System.EventHandler(this.ExportMidi_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_137_cogwheel;
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.Settings_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Image = global::Nebulator.Properties.Resources.glyphicons_195_question_sign;
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(138, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.About_Click);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerMain.IsSplitterFixed = true;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 24);
            this.splitContainerMain.Name = "splitContainerMain";
            this.splitContainerMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.AutoScroll = true;
            this.splitContainerMain.Panel1.Controls.Add(this.chkSeq);
            this.splitContainerMain.Panel1.Controls.Add(this.chkUi);
            this.splitContainerMain.Panel1.Controls.Add(this.btnCompile);
            this.splitContainerMain.Panel1.Controls.Add(this.timeMaster);
            this.splitContainerMain.Panel1.Controls.Add(this.sldVolume);
            this.splitContainerMain.Panel1.Controls.Add(this.chkPlay);
            this.splitContainerMain.Panel1.Controls.Add(this.potSpeed);
            this.splitContainerMain.Panel1.Controls.Add(this.btnRewind);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.splitContainerControl);
            this.splitContainerMain.Size = new System.Drawing.Size(826, 556);
            this.splitContainerMain.TabIndex = 14;
            // 
            // chkSeq
            // 
            this.chkSeq.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkSeq.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkSeq.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkSeq.Location = new System.Drawing.Point(88, 25);
            this.chkSeq.Name = "chkSeq";
            this.chkSeq.Size = new System.Drawing.Size(21, 19);
            this.chkSeq.TabIndex = 41;
            this.chkSeq.Text = "S";
            this.toolTip.SetToolTip(this.chkSeq, "Play declaritive sequences when play is pushed");
            this.chkSeq.UseVisualStyleBackColor = true;
            // 
            // chkUi
            // 
            this.chkUi.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkUi.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkUi.Font = new System.Drawing.Font("Microsoft Sans Serif", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkUi.Location = new System.Drawing.Point(88, 5);
            this.chkUi.Name = "chkUi";
            this.chkUi.Size = new System.Drawing.Size(21, 19);
            this.chkUi.TabIndex = 40;
            this.chkUi.Text = "U";
            this.toolTip.SetToolTip(this.chkUi, "Run graphics continuously");
            this.chkUi.UseVisualStyleBackColor = true;
            // 
            // btnCompile
            // 
            this.btnCompile.FlatAppearance.BorderSize = 0;
            this.btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCompile.Image = global::Nebulator.Properties.Resources.glyphicons_366_restart;
            this.btnCompile.Location = new System.Drawing.Point(116, 7);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(34, 32);
            this.btnCompile.TabIndex = 38;
            this.toolTip.SetToolTip(this.btnCompile, "Compile neb file - lit indicates file changed externally");
            this.btnCompile.UseVisualStyleBackColor = false;
            this.btnCompile.Click += new System.EventHandler(this.Compile_Click);
            // 
            // timeMaster
            // 
            this.timeMaster.ControlColor = System.Drawing.Color.Orange;
            time1.Tick = 0;
            time1.Tock = 0;
            this.timeMaster.CurrentTime = time1;
            this.timeMaster.Font = new System.Drawing.Font("Consolas", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.timeMaster.Location = new System.Drawing.Point(269, 7);
            this.timeMaster.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            this.timeMaster.MaxTick = 0;
            this.timeMaster.Name = "timeMaster";
            this.timeMaster.ShowProgress = true;
            this.timeMaster.Size = new System.Drawing.Size(175, 34);
            this.timeMaster.TabIndex = 37;
            this.timeMaster.ValueChanged += new System.EventHandler(this.Time_ValueChanged);
            // 
            // sldVolume
            // 
            this.sldVolume.ControlColor = System.Drawing.Color.Orange;
            this.sldVolume.Label = "vol";
            this.sldVolume.Location = new System.Drawing.Point(196, 7);
            this.sldVolume.Maximum = 200;
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
            this.chkPlay.Location = new System.Drawing.Point(49, 7);
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
            this.potSpeed.Location = new System.Drawing.Point(155, 7);
            this.potSpeed.Maximum = 200D;
            this.potSpeed.Minimum = 30D;
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
            this.btnRewind.Location = new System.Drawing.Point(10, 7);
            this.btnRewind.Name = "btnRewind";
            this.btnRewind.Size = new System.Drawing.Size(34, 32);
            this.btnRewind.TabIndex = 31;
            this.toolTip.SetToolTip(this.btnRewind, "Reset to start");
            this.btnRewind.UseVisualStyleBackColor = false;
            this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);
            // 
            // splitContainerControl
            // 
            this.splitContainerControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerControl.Location = new System.Drawing.Point(0, 0);
            this.splitContainerControl.Name = "splitContainerControl";
            this.splitContainerControl.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerControl.Panel1
            // 
            this.splitContainerControl.Panel1.Controls.Add(this.levers);
            // 
            // splitContainerControl.Panel2
            // 
            this.splitContainerControl.Panel2.Controls.Add(this.infoDisplay);
            this.splitContainerControl.Size = new System.Drawing.Size(826, 502);
            this.splitContainerControl.SplitterDistance = 43;
            this.splitContainerControl.TabIndex = 2;
            // 
            // levers
            // 
            this.levers.BackColor = System.Drawing.Color.AliceBlue;
            this.levers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.levers.Location = new System.Drawing.Point(0, 0);
            this.levers.Name = "levers";
            this.levers.Size = new System.Drawing.Size(826, 43);
            this.levers.TabIndex = 0;
            // 
            // infoDisplay
            // 
            this.infoDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.infoDisplay.Location = new System.Drawing.Point(0, 0);
            this.infoDisplay.MaxLength = 5000;
            this.infoDisplay.Name = "infoDisplay";
            this.infoDisplay.Size = new System.Drawing.Size(826, 455);
            this.infoDisplay.TabIndex = 0;
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(826, 580);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Nebulator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerControl.Panel1.ResumeLayout(false);
            this.splitContainerControl.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerControl)).EndInit();
            this.splitContainerControl.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pianoToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainerMain;
        private Controls.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private Controls.Pot potSpeed;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.SplitContainer splitContainerControl;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private Levers levers;
        private Controls.TimeControl timeMaster;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.ToolStripMenuItem exportMidiToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importMidiToolStripMenuItem;
        private InfoDisplay infoDisplay;
        private System.Windows.Forms.ToolStripMenuItem killMidiToolStripMenuItem;
        private System.Windows.Forms.CheckBox chkSeq;
        private System.Windows.Forms.CheckBox chkUi;
    }
}

