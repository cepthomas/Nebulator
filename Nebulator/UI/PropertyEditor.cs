using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;


namespace Nebulator.UI
{
    /// <summary>A simple general purpose editor for properties.</summary>
    public class PropertyEditor : Form
    {
        /// <summary>Inputs.</summary>
        public object EditObject { get; set; } = null;

        /// <summary>Edit flag.</summary>
        public bool Dirty { get; set; } = false;

        /// <summary>Constructor.</summary>
        public PropertyEditor()
        {
            InitializeComponent();
        }

        /// <summary>Initialize the elements.</summary>
        private void PropertyEditor_Load(object sender, EventArgs e)
        {
            if (EditObject != null)
            {
                pgEdit.SelectedObject = EditObject;
            }

            //pgEdit.ExpandAllGridItems();
            pgEdit.PropertyValueChanged += PgEdit_PropertyValueChanged;
            //pgEdit.PropertyGridExEvent += ...
            //pgEdit.AddButton(...);
        }

        /// <summary>User accepts selections.</summary>
        private void PgEdit_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Dirty = true;
        }

        /// <summary>User accepts selections.</summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
        }

        /// <summary>User rejects selections.</summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Dirty = false;
        }

        /// <summary>Required designer variable.</summary>
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
            this.components = new System.ComponentModel.Container();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pgEdit = new Nebulator.Controls.PropertyGridEx();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(92, 306);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(198, 306);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // pgEdit
            // 
            this.pgEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            // 
            // 
            // 
            this.pgEdit.DocCommentDescription.Location = new System.Drawing.Point(3, 18);
            this.pgEdit.DocCommentDescription.Name = "";
            this.pgEdit.DocCommentDescription.Size = new System.Drawing.Size(332, 37);
            this.pgEdit.DocCommentDescription.TabIndex = 1;
            this.pgEdit.DocCommentImage = null;
            // 
            // 
            // 
            this.pgEdit.DocCommentTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.pgEdit.DocCommentTitle.Location = new System.Drawing.Point(3, 3);
            this.pgEdit.DocCommentTitle.Name = "";
            this.pgEdit.DocCommentTitle.Size = new System.Drawing.Size(332, 15);
            this.pgEdit.DocCommentTitle.TabIndex = 0;
            this.pgEdit.LineColor = System.Drawing.SystemColors.ControlDark;
            this.pgEdit.Location = new System.Drawing.Point(12, 12);
            this.pgEdit.Name = "pgEdit";
            this.pgEdit.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.pgEdit.Size = new System.Drawing.Size(338, 276);
            this.pgEdit.TabIndex = 3;
            // 
            // 
            // 
            this.pgEdit.ToolStrip.Location = new System.Drawing.Point(0, 0);
            this.pgEdit.ToolStrip.Name = "";
            this.pgEdit.ToolStrip.Size = new System.Drawing.Size(338, 25);
            this.pgEdit.ToolStrip.TabIndex = 1;
            // 
            // PropertyEditor
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(362, 346);
            this.Controls.Add(this.pgEdit);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "PropertyEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "PropertyEditor";
            this.Load += new System.EventHandler(this.PropertyEditor_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private Controls.PropertyGridEx pgEdit;
    }
}
