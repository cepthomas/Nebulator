﻿namespace Nebulator.Script
{
    partial class Surface
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
            this.SuspendLayout();
            // 
            // Surface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "Surface";
            this.Size = new System.Drawing.Size(237, 204);
            this.Enter += new System.EventHandler(this.Surface_Enter);
            this.Leave += new System.EventHandler(this.Surface_Leave);
            this.Resize += new System.EventHandler(this.Surface_Resize);
            this.ResumeLayout(false);
        }

        #endregion
    }
}