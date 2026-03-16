namespace Digital_Weighing_Scale
{
    partial class SyncOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SyncOptions));
            this.btnYES = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.ConnectToLAN = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // btnYES
            // 
            this.btnYES.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnYES.Location = new System.Drawing.Point(168, 86);
            this.btnYES.Name = "btnYES";
            this.btnYES.Size = new System.Drawing.Size(75, 23);
            this.btnYES.TabIndex = 0;
            this.btnYES.Text = "Yes";
            this.btnYES.UseVisualStyleBackColor = true;
            this.btnYES.Click += new System.EventHandler(this.btnYES_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(249, 86);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.TabStop = false;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ConnectToLAN
            // 
            this.ConnectToLAN.BackColor = System.Drawing.Color.Green;
            this.ConnectToLAN.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ConnectToLAN.Location = new System.Drawing.Point(24, 86);
            this.ConnectToLAN.Name = "ConnectToLAN";
            this.ConnectToLAN.Size = new System.Drawing.Size(91, 23);
            this.ConnectToLAN.TabIndex = 3;
            this.ConnectToLAN.TabStop = false;
            this.ConnectToLAN.Text = "Connect to LAN";
            this.ConnectToLAN.UseVisualStyleBackColor = false;
            this.ConnectToLAN.Click += new System.EventHandler(this.ConnectToLAN_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(12, 12);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(324, 103);
            this.richTextBox1.TabIndex = 5;
            this.richTextBox1.TabStop = false;
            this.richTextBox1.Text = "Select the latest saved file to sync the local database. This will overwrite exis" +
    "ting data with server updates.\nAlternatively, connect to the main LAN.\n\nProceed?" +
    "";
            // 
            // SyncOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(348, 127);
            this.Controls.Add(this.ConnectToLAN);
            this.Controls.Add(this.btnYES);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.richTextBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SyncOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.SyncOptions_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnYES;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button ConnectToLAN;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}