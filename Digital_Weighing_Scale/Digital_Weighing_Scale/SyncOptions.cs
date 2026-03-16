using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Digital_Weighing_Scale
{
    public partial class SyncOptions : Form
    {
        public enum SyncChoice
        {
            None, 
            SyncLocalTrufile,
            ConnectServerTruLAN
        }

        public SyncChoice UserChoice { get; set; } = SyncChoice.None;
        public SyncOptions()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            this.KeyPreview = true;
            this.KeyDown += CancelButtonSync;
            this.KeyDown += YesToSync;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void CancelButtonSync (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Escape)
            {
                btnCancel_Click(sender, e);
            }
        }
        private void YesToSync (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                btnYES_Click(sender, e);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void btnYES_Click(object sender, EventArgs e)
        {

            try
            {

                byte[] AES_KEY = Encoding.UTF8.GetBytes("A1B2C3D4E5F6G7H8"); // these should be the same encryption-decryption value in scrap sale system
                byte[] AES_IV = Encoding.UTF8.GetBytes("H8G7F6E5D4C3B2A1");



                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select the SCRAPENC file";
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                ofd.Filter = "Encrypted Scrap Database (*.scrapenc)|*.scrapenc";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                string encryptedPath = ofd.FileName;
                byte[] encryptedData = File.ReadAllBytes(encryptedPath);

                // Decrypt
                string decryptedSQL = DecryptAES(encryptedData, AES_KEY, AES_IV);

                // Local database connection
                // string connectionStringLocal = "server=localhost;user=root;password=Windows7;database=Scraps";
                string connectionStringLocal = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
                using (var connection = new MySqlConnection(connectionStringLocal))

                {
                    connection.Open();

                    using (var cmd = new MySqlCommand("", connection))
                    {

                        cmd.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
                        cmd.ExecuteNonQuery();


                        string[] tables = new string[]
                        {

                    "InChargePersonnelDetails",
                    "AdminStaffMasterfile",
                    "CustomerMasterfile",
                    "PriceList",
                    "ItemMasterfile",
                    "CategoryMasterfile",
                    "BoxTypeMasterfile",
                    "CustomerTypeMasterfile",
                    "AccessTypeMasterfile"
                        };

                        foreach (var table in tables)
                        {
                            cmd.CommandText = $"TRUNCATE TABLE {table};";
                            cmd.ExecuteNonQuery();
                        }


                        cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1;";
                        cmd.ExecuteNonQuery();


                        cmd.CommandText = decryptedSQL;
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Local database synced successfully!", "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error syncing database: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void CopyTable(MySqlConnection sourceConn, MySqlConnection targetConn, string tableName)
        {
            using (var selectCmd = new MySqlCommand($"SELECT * FROM {tableName}", sourceConn))
            using (var reader = selectCmd.ExecuteReader())
            {
                DataTable dt = new DataTable();
                dt.Load(reader);

                foreach (DataRow row in dt.Rows)
                {
                    var columns = string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
                    var parameters = string.Join(",", dt.Columns.Cast<DataColumn>().Select(c => "@" + c.ColumnName));

                    string insertSql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";

                    using (var insertCmd = new MySqlCommand(insertSql, targetConn))
                    {
                        foreach (DataColumn col in dt.Columns)
                        {
                            insertCmd.Parameters.AddWithValue("@" + col.ColumnName, row[col]);
                        }
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ConnectToLAN_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Sync Updates from the DB Server using LAN connection\n\n" +
                "This will overwrite existing local data with the latest server data.\n\n" +
                "Do you want to proceed?",
                "Sync via LAN",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                string serverConnStr = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
                string localConnStr = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;

                using (var serverConn = new MySqlConnection(serverConnStr))
                using (var localConn = new MySqlConnection(localConnStr))
                {
                    serverConn.Open();
                    localConn.Open();

                    using (var transaction = localConn.BeginTransaction())
                    using (var cmd = new MySqlCommand("", localConn, transaction))
                    {
                     
                        cmd.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
                        cmd.ExecuteNonQuery();

                        string[] tables =
                        {
                    "InChargePersonnelDetails",
                    "AdminStaffMasterfile",
                    "CustomerMasterfile",
                    "PriceList",
                    "ItemMasterfile",
                    "CategoryMasterfile",
                    "BoxTypeMasterfile",
                    "CustomerTypeMasterfile",
                    "AccessTypeMasterfile"
                };

                      
                        foreach (var table in tables)
                        {
                            cmd.CommandText = $"TRUNCATE TABLE {table};";
                            cmd.ExecuteNonQuery();
                        }

                        
                        foreach (var table in tables.Reverse()) 
                        {
                            CopyTable(serverConn, localConn, table);
                        }

                     
                        cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1;";
                        cmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                }

                MessageBox.Show("LAN sync completed successfully!", "Sync Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LAN sync failed:\n" + ex.Message,
                    "Sync Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SyncOptions_Load(object sender, EventArgs e){ }
    }
}
