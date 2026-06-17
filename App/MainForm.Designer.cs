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
            components = new System.ComponentModel.Container();
            sldVolume = new Ephemera.NBagOfUis.Slider();
            sldTempo = new Ephemera.NBagOfUis.Slider();
            toolTip = new System.Windows.Forms.ToolTip(components);
            btnCompile = new System.Windows.Forms.Button();
            btnRewind = new System.Windows.Forms.Button();
            chkPlay = new System.Windows.Forms.CheckBox();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            fileDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportMidiToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            showDefinitionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            btnMonIn = new System.Windows.Forms.ToolStripButton();
            btnMonOut = new System.Windows.Forms.ToolStripButton();
            btnKillComm = new System.Windows.Forms.ToolStripButton();
            btnAbout = new System.Windows.Forms.ToolStripButton();
            btnSettings = new System.Windows.Forms.ToolStripButton();
            tvInfo = new Ephemera.NBagOfUis.TextViewer();
            timeBar = new Ephemera.MidiLib.TimeBar();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // sldVolume
            // 
            sldVolume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldVolume.DrawColor = System.Drawing.Color.Red;
            sldVolume.Label = "vol";
            sldVolume.Location = new System.Drawing.Point(267, 47);
            sldVolume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            sldVolume.Maximum = 1D;
            sldVolume.Minimum = 0D;
            sldVolume.Name = "sldVolume";
            sldVolume.Orientation = System.Windows.Forms.Orientation.Horizontal;
            sldVolume.Resolution = 0.05D;
            sldVolume.Size = new System.Drawing.Size(88, 50);
            sldVolume.TabIndex = 36;
            toolTip.SetToolTip(sldVolume, "Master volume");
            sldVolume.Value = 1D;
            sldVolume.ValueChanged += Volume_ValueChanged;
            // 
            // sldTempo
            // 
            sldTempo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            sldTempo.DrawColor = System.Drawing.Color.LightGray;
            sldTempo.Label = "bpm";
            sldTempo.Location = new System.Drawing.Point(170, 47);
            sldTempo.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            sldTempo.Maximum = 200D;
            sldTempo.Minimum = 30D;
            sldTempo.Name = "sldTempo";
            sldTempo.Orientation = System.Windows.Forms.Orientation.Horizontal;
            sldTempo.Resolution = 5D;
            sldTempo.Size = new System.Drawing.Size(88, 50);
            sldTempo.TabIndex = 33;
            toolTip.SetToolTip(sldTempo, "Speed in BPM");
            sldTempo.Value = 100D;
            sldTempo.ValueChanged += Speed_ValueChanged;
            // 
            // toolTip
            // 
            toolTip.AutomaticDelay = 0;
            toolTip.AutoPopDelay = 0;
            toolTip.InitialDelay = 300;
            toolTip.ReshowDelay = 0;
            toolTip.UseAnimation = false;
            toolTip.UseFading = false;
            // 
            // btnCompile
            // 
            btnCompile.FlatAppearance.BorderSize = 0;
            btnCompile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnCompile.Image = Properties.Resources.glyphicons_366_restart;
            btnCompile.Location = new System.Drawing.Point(116, 47);
            btnCompile.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnCompile.Name = "btnCompile";
            btnCompile.Size = new System.Drawing.Size(45, 47);
            btnCompile.TabIndex = 38;
            toolTip.SetToolTip(btnCompile, "Compile script file - lit indicates file changed externally");
            btnCompile.UseVisualStyleBackColor = false;
            btnCompile.Click += Compile_Click;
            // 
            // btnRewind
            // 
            btnRewind.FlatAppearance.BorderSize = 0;
            btnRewind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRewind.Image = Properties.Resources.glyphicons_173_rewind;
            btnRewind.Location = new System.Drawing.Point(13, 47);
            btnRewind.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            btnRewind.Name = "btnRewind";
            btnRewind.Size = new System.Drawing.Size(45, 47);
            btnRewind.TabIndex = 31;
            toolTip.SetToolTip(btnRewind, "Reset to start");
            btnRewind.UseVisualStyleBackColor = false;
            btnRewind.Click += Rewind_Click;
            // 
            // chkPlay
            // 
            chkPlay.Appearance = System.Windows.Forms.Appearance.Button;
            chkPlay.BackColor = System.Drawing.SystemColors.Control;
            chkPlay.FlatAppearance.BorderSize = 0;
            chkPlay.FlatAppearance.CheckedBackColor = System.Drawing.Color.FromArgb(255, 128, 128);
            chkPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            chkPlay.Image = Properties.Resources.glyphicons_174_play;
            chkPlay.Location = new System.Drawing.Point(65, 47);
            chkPlay.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            chkPlay.MaximumSize = new System.Drawing.Size(43, 47);
            chkPlay.MinimumSize = new System.Drawing.Size(43, 47);
            chkPlay.Name = "chkPlay";
            chkPlay.Size = new System.Drawing.Size(43, 47);
            chkPlay.TabIndex = 35;
            toolTip.SetToolTip(chkPlay, "Play project");
            chkPlay.UseVisualStyleBackColor = false;
            chkPlay.Click += Play_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileDropDownButton, btnMonIn, btnMonOut, btnKillComm, btnAbout, btnSettings });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(813, 27);
            toolStrip1.TabIndex = 39;
            toolStrip1.Text = "toolStrip1";
            // 
            // fileDropDownButton
            // 
            fileDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            fileDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openToolStripMenuItem, recentToolStripMenuItem, exportMidiToolStripMenuItem, showDefinitionsToolStripMenuItem });
            fileDropDownButton.Image = Properties.Resources.glyphicons_37_file;
            fileDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            fileDropDownButton.Name = "fileDropDownButton";
            fileDropDownButton.Size = new System.Drawing.Size(34, 24);
            fileDropDownButton.Text = "fileDropDownButton";
            fileDropDownButton.ToolTipText = "File operations";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new System.Drawing.Size(206, 24);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += Open_Click;
            // 
            // recentToolStripMenuItem
            // 
            recentToolStripMenuItem.Name = "recentToolStripMenuItem";
            recentToolStripMenuItem.Size = new System.Drawing.Size(206, 24);
            recentToolStripMenuItem.Text = "Recent";
            // 
            // exportMidiToolStripMenuItem
            // 
            exportMidiToolStripMenuItem.Enabled = false;
            exportMidiToolStripMenuItem.Name = "exportMidiToolStripMenuItem";
            exportMidiToolStripMenuItem.Size = new System.Drawing.Size(206, 24);
            exportMidiToolStripMenuItem.Text = "Export Midi";
            exportMidiToolStripMenuItem.Click += ExportMidi_Click;
            // 
            // showDefinitionsToolStripMenuItem
            // 
            showDefinitionsToolStripMenuItem.Name = "showDefinitionsToolStripMenuItem";
            showDefinitionsToolStripMenuItem.Size = new System.Drawing.Size(206, 24);
            showDefinitionsToolStripMenuItem.Text = "Show Definitions";
            showDefinitionsToolStripMenuItem.Click += ShowDefinitions_Click;
            // 
            // btnMonIn
            // 
            btnMonIn.CheckOnClick = true;
            btnMonIn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnMonIn.Image = Properties.Resources.glyphicons_213_arrow_down;
            btnMonIn.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnMonIn.Name = "btnMonIn";
            btnMonIn.Size = new System.Drawing.Size(26, 24);
            btnMonIn.Text = "btnMonIn";
            btnMonIn.ToolTipText = "Monitor messages in";
            // 
            // btnMonOut
            // 
            btnMonOut.CheckOnClick = true;
            btnMonOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnMonOut.Image = Properties.Resources.glyphicons_214_arrow_up;
            btnMonOut.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnMonOut.Name = "btnMonOut";
            btnMonOut.Size = new System.Drawing.Size(26, 24);
            btnMonOut.Text = "btnMonOut";
            btnMonOut.ToolTipText = "Monitor messages out";
            // 
            // btnKillComm
            // 
            btnKillComm.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnKillComm.Image = Properties.Resources.glyphicons_206_electricity;
            btnKillComm.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnKillComm.Name = "btnKillComm";
            btnKillComm.Size = new System.Drawing.Size(26, 24);
            btnKillComm.Text = "btnKillComm";
            btnKillComm.ToolTipText = "Kill all devices";
            // 
            // btnAbout
            // 
            btnAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnAbout.Image = Properties.Resources.glyphicons_195_question_sign;
            btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new System.Drawing.Size(26, 24);
            btnAbout.Text = "btnAbout";
            btnAbout.ToolTipText = "About";
            // 
            // btnSettings
            // 
            btnSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            btnSettings.Image = Properties.Resources.glyphicons_137_cogwheel;
            btnSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new System.Drawing.Size(26, 24);
            btnSettings.Text = "btnSettings";
            btnSettings.ToolTipText = "Edit settings";
            // 
            // tvInfo
            // 
            tvInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tvInfo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            tvInfo.Location = new System.Drawing.Point(13, 580);
            tvInfo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            tvInfo.MatchUseBackground = true;
            tvInfo.MaxText = 5000;
            tvInfo.Name = "tvInfo";
            tvInfo.Prompt = "";
            tvInfo.Size = new System.Drawing.Size(789, 206);
            tvInfo.TabIndex = 41;
            tvInfo.WordWrap = true;
            // 
            // timeBar
            // 
            timeBar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            timeBar.DrawColor = System.Drawing.Color.Red;
            timeBar.GridLines = 0;
            timeBar.Location = new System.Drawing.Point(12, 117);
            timeBar.Name = "timeBar";
            timeBar.SelectedColor = System.Drawing.Color.Blue;
            timeBar.Size = new System.Drawing.Size(790, 52);
            timeBar.Snap = Ephemera.MidiLib.SnapType.Beat;
            timeBar.TabIndex = 44;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.Control;
            BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            ClientSize = new System.Drawing.Size(813, 796);
            Controls.Add(timeBar);
            Controls.Add(tvInfo);
            Controls.Add(toolStrip1);
            Controls.Add(btnCompile);
            Controls.Add(sldVolume);
            Controls.Add(sldTempo);
            Controls.Add(btnRewind);
            Controls.Add(chkPlay);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "MainForm";
            Text = "Nebulator";
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private Ephemera.NBagOfUis.Slider sldVolume;
        private System.Windows.Forms.CheckBox chkPlay;
        private Ephemera.NBagOfUis.Slider sldTempo;
        private System.Windows.Forms.Button btnRewind;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnMonIn;
        private System.Windows.Forms.ToolStripButton btnMonOut;
        private System.Windows.Forms.ToolStripDropDownButton fileDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportMidiToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnKillComm;
        private Ephemera.NBagOfUis.TextViewer tvInfo;
        private Ephemera.MidiLib.TimeBar timeBar;
        private System.Windows.Forms.ToolStripMenuItem showDefinitionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton btnAbout;
        private System.Windows.Forms.ToolStripButton btnSettings;
    }
}

