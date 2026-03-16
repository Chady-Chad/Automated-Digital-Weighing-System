using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace Digital_Weighing_Scale
{
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public partial class Main_Menu: Form
    {

       
        public Main_Menu()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += ToTheNewTransaction;
            this.KeyDown += ToGenerateReport;
            this.KeyDown += ToDownloadRecord;
            this.KeyDown += SyncUpdates;
            this.StartPosition = FormStartPosition.CenterScreen;
           

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Using Function key F1======================================================
        private void ToTheNewTransaction (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F1)
            {
                string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();

                if (availablePorts.Length == 0)
                {
                    MessageBox.Show(
                        "No scale device is connected to the PC. Please connect, turn on the scale, and try again.",
                        "Scale Not Detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }

                //=============================================================================================
                // try to detect the scale specifically by attempting to open a port
                bool scaleFound = false;
                foreach (string portName in availablePorts)
                {
                    try
                    {
                        using (var testPort = new System.IO.Ports.SerialPort(portName, 9600))
                        {
                            testPort.Open();
                            if (testPort.IsOpen)
                            {
                                scaleFound = true;
                                testPort.Close();
                                break;
                            }
                        }
                    }
                    catch
                    {

                    }
                }

                if (!scaleFound)
                {
                    MessageBox.Show(
                        "No scale device is connected or accessible. Please check the connection and try again.",
                        "Scale Not Detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }


                /* New_Transaction newForm = new New_Transaction(this);
                 newForm.Show();
                 this.Hide();
                */
                this.KeyDown -= ToGenerateReport;
                this.KeyDown -= ToDownloadRecord;
                

                LoadChildForm(new New_Transaction(this));
            }
        }
        //Using Button Mouse Click===================================================
        /* private void button1_Click(object sender, EventArgs e)
         {
             New_Transaction helpForm = new New_Transaction(this);
             helpForm.Show();
             this.Hide();
         }*/
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /* private void LoadChildForm(Form childForm)
         {
             panel3.Controls.Clear();
             childForm.TopLevel = false;
             childForm.FormBorderStyle = FormBorderStyle.None;
             childForm.Dock = DockStyle.Fill;
             panel3.Controls.Add(childForm);
             panel3.Tag = childForm;
             childForm.BringToFront();
             childForm.Show();
         }
        */
        private void LoadChildForm(Form childForm)
        {
         
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl != panel3)
                    ctrl.Visible = false;
            }

            panel3.Controls.Clear();
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            panel3.Controls.Add(childForm);
            panel3.Tag = childForm;
            childForm.BringToFront();
            childForm.Show();
        }
        public void ShowMainMenuUI()
        {
            foreach (Control ctrl in this.Controls)
            {
                ctrl.Visible = true;
            }
            panel3.Controls.Clear();
            this.KeyDown += ToGenerateReport;
            this.KeyDown += ToDownloadRecord;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Keyboard press function below


      
        private void ToGenerateReport(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5) {
                button5_Click(sender, e);
            }
        }
        private void ToDownloadRecord(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                button3_Click(sender, e);
            }
        }

        private void SyncUpdates(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F4)
            {
                SyncDatabaseFromtheMain_Click(sender, e);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            
            string[] availablePorts = System.IO.Ports.SerialPort.GetPortNames();

            if (availablePorts.Length == 0)
            {
                MessageBox.Show(
                    "No scale device is connected to the PC. Please connect, turn on the scale, and try again.",
                    "Scale Not Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return; 
            }
            //=============================================================================================
            // try to detect the scale specifically by attempting to open a port
            bool scaleFound = false;
            foreach (string portName in availablePorts)
            {
                try
                {
                    using (var testPort = new System.IO.Ports.SerialPort(portName, 9600))
                    {
                        testPort.Open();
                        if (testPort.IsOpen)
                        {
                            scaleFound = true;
                            testPort.Close();
                            break;
                        }
                    }
                }
                catch
                {
                 
                }
            }

            if (!scaleFound)
            {
                MessageBox.Show(
                    "No scale device is connected or accessible. Please check the connection and try again.",
                    "Scale Not Detected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }


            /*New_Transaction newForm = new New_Transaction(this);
            newForm.Show();
            this.Hide();
            */
            this.KeyDown -= ToGenerateReport;
            this.KeyDown -= ToDownloadRecord;
            LoadChildForm(new New_Transaction(this));
           

        }
        //=====================================================================================================
        private void button5_Click(object sender, EventArgs e)
        {
            GenerateReport reportForm = new GenerateReport();
            reportForm.Show();
        }
        //===========================================================================
        private void button3_Click(object sender, EventArgs e)
        {
         ExportForm exportExcel = new ExportForm();
            exportExcel.ShowDialog();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            AboutBox1 about = new AboutBox1(this);
            about.ShowDialog();

        }

        private void SyncDatabaseFromtheMain_Click(object sender, EventArgs e)
        {
            SyncOptions SyncLAN = new SyncOptions();
            SyncLAN.ShowDialog();
            //DialogResult showDat = MessageBox.Show(
            //    "To sync the local database, select the latest encrypted SCRAPENC file from PC Downloads.\n\n" +
            //    "This will overwrite existing local data with the latest server data.\n\n" +
            //    "Do you want to proceed?",
            //    "Updates",
            //    MessageBoxButtons.YesNo,
            //    MessageBoxIcon.Information);

            //if (showDat != DialogResult.Yes)
            //    return;

            //try
            //{

            //    byte[] AES_KEY = Encoding.UTF8.GetBytes("A1B2C3D4E5F6G7H8"); // these should be the same encryption-decryption value in scrap sale system
            //    byte[] AES_IV = Encoding.UTF8.GetBytes("H8G7F6E5D4C3B2A1");
                


            //    OpenFileDialog ofd = new OpenFileDialog();
            //    ofd.Title = "Select the SCRAPENC file";
            //    ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            //    ofd.Filter = "Encrypted Scrap Database (*.scrapenc)|*.scrapenc";

            //    if (ofd.ShowDialog() != DialogResult.OK)
            //        return;

            //    string encryptedPath = ofd.FileName;
            //    byte[] encryptedData = File.ReadAllBytes(encryptedPath);

            //    // Decrypt
            //    string decryptedSQL = DecryptAES(encryptedData, AES_KEY, AES_IV);

            //    // Local database connection
            //   // string connectionStringLocal = "server=localhost;user=root;password=Windows7;database=Scraps";
            //    string connectionStringLocal = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
            //    using (var connection = new MySqlConnection(connectionStringLocal))

            //    {
            //        connection.Open();

            //        using (var cmd = new MySqlCommand("", connection))
            //        {

            //            cmd.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
            //            cmd.ExecuteNonQuery();


            //            string[] tables = new string[]
            //            {

            //        "InChargePersonnelDetails",
            //        "AdminStaffMasterfile",
            //        "CustomerMasterfile",
            //        "PriceList",
            //        "ItemMasterfile",
            //        "CategoryMasterfile",
            //        "BoxTypeMasterfile",
            //        "CustomerTypeMasterfile",
            //        "AccessTypeMasterfile"
            //            };

            //            foreach (var table in tables)
            //            {
            //                cmd.CommandText = $"TRUNCATE TABLE {table};";
            //                cmd.ExecuteNonQuery();
            //            }


            //            cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1;";
            //            cmd.ExecuteNonQuery();


            //            cmd.CommandText = decryptedSQL;
            //            cmd.ExecuteNonQuery();
            //        }
            //    }

            //    MessageBox.Show("Local database synced successfully!", "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("Error syncing database: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}
        }

        // ---------------- AES Decrypt Helper ----------------
        private string DecryptAES(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (var ms = new MemoryStream(cipherText))
                using (var cryptoStream = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //isolate these voids for future functions
        private void Main_Menu_Load(object sender, EventArgs e){ }
        private void button2_Click(object sender, EventArgs e){ }
        private void pictureBox1_Click(object sender, EventArgs e){ }
        private void panel2_Paint(object sender, PaintEventArgs e){ }
        private void panel3_Paint(object sender, PaintEventArgs e)
        {
            
        }

    }
}
