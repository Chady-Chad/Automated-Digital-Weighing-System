namespace Digital_Weighing_Scale
{
    partial class ExportForm
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
            this.DateRecord = new System.Windows.Forms.DateTimePicker();
            this.btnDownload = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.dataGridRecordedDates = new System.Windows.Forms.DataGridView();
            this.label2 = new System.Windows.Forms.Label();
            this.Sync_MainTru_Lan = new System.Windows.Forms.Button();
            this.DownloadTruExcel = new System.Windows.Forms.LinkLabel();
            this.label3 = new System.Windows.Forms.Label();
            this.SelectTransaction = new System.Windows.Forms.ComboBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordedDates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // DateRecord
            // 
            this.DateRecord.Location = new System.Drawing.Point(128, 200);
            this.DateRecord.Name = "DateRecord";
            this.DateRecord.Size = new System.Drawing.Size(200, 20);
            this.DateRecord.TabIndex = 0;
            this.DateRecord.ValueChanged += new System.EventHandler(this.DateRecord_ValueChanged);
            // 
            // btnDownload
            // 
            this.btnDownload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.btnDownload.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDownload.Location = new System.Drawing.Point(159, 226);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(141, 40);
            this.btnDownload.TabIndex = 1;
            this.btnDownload.Text = "Download Transaction data";
            this.btnDownload.UseVisualStyleBackColor = false;
            this.btnDownload.Click += new System.EventHandler(this.btnDownload_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(170, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Recorded Transaction Dates";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(97, 344);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(258, 29);
            this.progressBar1.TabIndex = 3;
            this.progressBar1.Click += new System.EventHandler(this.progressBar1_Click);
            // 
            // dataGridRecordedDates
            // 
            this.dataGridRecordedDates.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dataGridRecordedDates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridRecordedDates.Location = new System.Drawing.Point(12, 25);
            this.dataGridRecordedDates.Name = "dataGridRecordedDates";
            this.dataGridRecordedDates.Size = new System.Drawing.Size(426, 159);
            this.dataGridRecordedDates.TabIndex = 4;
            this.dataGridRecordedDates.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridRecordedDates_CellContentClick);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Blue;
            this.label2.Location = new System.Drawing.Point(214, 269);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 18);
            this.label2.TabIndex = 6;
            this.label2.Text = "OR";
            // 
            // Sync_MainTru_Lan
            // 
            this.Sync_MainTru_Lan.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.Sync_MainTru_Lan.Cursor = System.Windows.Forms.Cursors.Default;
            this.Sync_MainTru_Lan.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.Sync_MainTru_Lan.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Sync_MainTru_Lan.Location = new System.Drawing.Point(159, 290);
            this.Sync_MainTru_Lan.Name = "Sync_MainTru_Lan";
            this.Sync_MainTru_Lan.Size = new System.Drawing.Size(141, 37);
            this.Sync_MainTru_Lan.TabIndex = 7;
            this.Sync_MainTru_Lan.Text = "SYNC TO MAIN DB SERVER";
            this.Sync_MainTru_Lan.UseVisualStyleBackColor = false;
            this.Sync_MainTru_Lan.Click += new System.EventHandler(this.Sync_MainTru_Lan_Click);
            // 
            // DownloadTruExcel
            // 
            this.DownloadTruExcel.AutoSize = true;
            this.DownloadTruExcel.Location = new System.Drawing.Point(130, 409);
            this.DownloadTruExcel.Name = "DownloadTruExcel";
            this.DownloadTruExcel.Size = new System.Drawing.Size(211, 13);
            this.DownloadTruExcel.TabIndex = 9;
            this.DownloadTruExcel.TabStop = true;
            this.DownloadTruExcel.Text = "Download Transaction Record in Excel file \r\n";
            this.DownloadTruExcel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.DownloadTruExcel_LinkClicked);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Blue;
            this.label3.Location = new System.Drawing.Point(0, 200);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(122, 18);
            this.label3.TabIndex = 10;
            this.label3.Text = "Transaction date:";
            // 
            // SelectTransaction
            // 
            this.SelectTransaction.FormattingEnabled = true;
            this.SelectTransaction.Location = new System.Drawing.Point(3, 406);
            this.SelectTransaction.Name = "SelectTransaction";
            this.SelectTransaction.Size = new System.Drawing.Size(121, 21);
            this.SelectTransaction.TabIndex = 11;
            this.SelectTransaction.SelectedIndexChanged += new System.EventHandler(this.SelectTransaction_SelectedIndexChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBox1.Image = global::Digital_Weighing_Scale.Properties.Resources.icons8_download1;
            this.pictureBox1.Location = new System.Drawing.Point(306, 240);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(68, 68);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // ExportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.ClientSize = new System.Drawing.Size(450, 436);
            this.Controls.Add(this.SelectTransaction);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.DownloadTruExcel);
            this.Controls.Add(this.Sync_MainTru_Lan);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.dataGridRecordedDates);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnDownload);
            this.Controls.Add(this.DateRecord);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportForm";
            this.Load += new System.EventHandler(this.SelectDateForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridRecordedDates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker DateRecord;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.DataGridView dataGridRecordedDates;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button Sync_MainTru_Lan;
        private System.Windows.Forms.LinkLabel DownloadTruExcel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox SelectTransaction;
    }
}