using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using ClosedXML.Excel;

namespace Digital_Weighing_Scale
{
    public partial class ExportForm : Form
    {
        private static readonly byte[] AES_KEY = Encoding.UTF8.GetBytes("A1B2C3D4E5F6G7H8"); // 16 bytes
        private static readonly byte[] AES_IV = Encoding.UTF8.GetBytes("H8G7F6E5D4C3B2A1"); // 16 bytes

        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;

        DataTable masterTable = new DataTable();
        DataTable detailsTable = new DataTable();

        public ExportForm()
        {
            InitializeComponent();
            dataGridRecordedDates.ReadOnly = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            dataGridRecordedDates.CellClick += dataGridRecordedDates_CellClick;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool IsServerAvailable(string connStr)
        {
            try
            {
                using (var con = new MySqlConnection(connStr))
                {
                    con.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void InsertRow(
            MySqlConnection con,
            MySqlTransaction tx,
            string table,
            DataRow row)
        {
            var columns = row.Table.Columns.Cast<DataColumn>()
                .Where(c => row[c] != DBNull.Value)
                .ToList();

            string colNames = string.Join(",", columns.Select(c => $"`{c.ColumnName}`"));
            string paramNames = string.Join(",", columns.Select(c => $"@{c.ColumnName}"));

            string sql = $"INSERT IGNORE INTO `{table}` ({colNames}) VALUES ({paramNames})";

            using (var cmd = new MySqlCommand(sql, con, tx))
            {
                foreach (var col in columns)
                    cmd.Parameters.AddWithValue($"@{col.ColumnName}", row[col]);

                cmd.ExecuteNonQuery();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private async Task ShowLoadingCycle()
        {
            progressBar1.Visible = true;
            progressBar1.Value = 0;

            for (int i = 0; i <= 100; i += 10)
            {
                progressBar1.Value = i;
                await Task.Delay(200);
            }

            progressBar1.Visible = false;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private DataTable GetTable(string query, Dictionary<string, object> param = null, string connStr = null)
        {
            if (connStr == null)
                connStr = connectionString;

            using (MySqlConnection con = new MySqlConnection(connStr))
            {
                using (MySqlCommand cmd = new MySqlCommand(query, con))
                {
                    if (param != null)
                    {
                        foreach (var p in param)
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                    }

                    DataTable dt = new DataTable();
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SelectDateForm_Load(object sender, EventArgs e)
        {
            LoadTransactionList();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void LoadTransactionList()
        {
            string query = @"
        SELECT 
            `Transaction_No.` AS TransactionNo
        FROM TransactionMasterfile
        ORDER BY TransactionDate DESC";

            DataTable dt = GetTable(query);

            SelectTransaction.DataSource = dt;
            SelectTransaction.DisplayMember = "TransactionNo";   
            SelectTransaction.ValueMember = "TransactionNo";     
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SyncAllTransactions(string localConn, string serverConn)
        {
            using (var serverCon = new MySqlConnection(serverConn))
            {
                serverCon.Open();

                using (var tx = serverCon.BeginTransaction())
                {
                    try
                    {
                        // 1) Sync Lookup Tables FIRST
                        SyncLookupTable(serverCon, tx, localConn, "CustomerTypeMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "AccessTypeMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "CustomerMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "AdminStaffMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "CategoryMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "ItemMasterfile");
                        SyncLookupTable(serverCon, tx, localConn, "BoxTypeMasterfile");

                        // 2) Sync Transactions
                        DataTable allMasters = GetTable("SELECT * FROM TransactionMasterfile", null, localConn);

                        foreach (DataRow m in allMasters.Rows)
                        {
                            int transID = Convert.ToInt32(m["Transaction_ID"]);

                            // Insert TransactionMasterfile row
                            InsertRow(serverCon, tx, "TransactionMasterfile", m);

                            // Insert TransactionDetails rows for this transaction
                            DataTable details = GetTable(
                                "SELECT * FROM TransactionDetails WHERE TransactionID=@id",
                                new Dictionary<string, object> { { "@id", transID } },
                                localConn);

                            foreach (DataRow d in details.Rows)
                                InsertRow(serverCon, tx, "TransactionDetails", d);

                            DataTable cancelled = GetTable(
                                "SELECT * FROM CancelledTransaction WHERE TransactionID=@id",
                                new Dictionary<string, object> { { "@id", transID } },
                                localConn);

                            foreach (DataRow c in cancelled.Rows)
                                InsertRow(serverCon, tx, "CancelledTransaction", c);
                        }

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SyncLookupTable(
            MySqlConnection serverCon,
            MySqlTransaction tx,
            string localConn,
            string tableName)
        {
            DataTable lookup = GetTable($"SELECT * FROM {tableName}", null, localConn);

            foreach (DataRow row in lookup.Rows)
            {
                InsertRow(serverCon, tx, tableName, row);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private byte[] EncryptAES(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = AES_KEY;
                aes.IV = AES_IV;

                using (var ms = new MemoryStream())
                using (var cryptoStream = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                    writer.Close();
                    return ms.ToArray();
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string ConvertTableToSQL(string tableName, DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            foreach (DataRow row in dt.Rows)
            {
                sb.Append($"INSERT INTO `{tableName}` (");

                sb.Append(string.Join(", ", dt.Columns.Cast<DataColumn>()
                    .Select(c => $"`{c.ColumnName}`")));

                sb.Append(") VALUES (");

                sb.Append(string.Join(", ", dt.Columns.Cast<DataColumn>().Select(c =>
                {
                    object val = row[c];

                    if (val == DBNull.Value)
                        return "NULL";

                    if (val is string || val is DateTime)
                        return $"'{MySqlHelper.EscapeString(val.ToString())}'";

                    return val.ToString().Replace(",", ".");
                })));

                sb.AppendLine(");");
            }

            return sb.ToString();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void DateRecord_ValueChanged(object sender, EventArgs e)
        {
            DateTime selectedDate = DateRecord.Value.Date;

            string qMaster = @"
                SELECT 
                    m.Transaction_ID,
                    m.`Transaction_No.`,
                    m.TransactionDate,
                    c.CustomerName AS Customer,
                    ct.CustomerTypeName AS CustomerType,
                    a.AdminStaffName AS AdminStaff
                FROM TransactionMasterfile m
                LEFT JOIN CustomerMasterfile c ON m.CustomerID = c.Customer_ID
                LEFT JOIN CustomerTypeMasterfile ct ON c.CustomerTypeID = ct.CustomerType_ID
                LEFT JOIN AdminStaffMasterfile a ON m.AdminStaffID = a.AdminStaff_ID
                WHERE m.TransactionDate = @d
                ORDER BY m.`Transaction_No.`";

            var param = new Dictionary<string, object>() { { "@d", selectedDate } };

            masterTable = GetTable(qMaster, param);

            dataGridRecordedDates.DataSource = masterTable;

            if (dataGridRecordedDates.Columns.Contains("Transaction_ID"))
                dataGridRecordedDates.Columns["Transaction_ID"].Visible = false;

            if (dataGridRecordedDates.Columns.Contains("AdminStaff"))
                dataGridRecordedDates.Columns["AdminStaff"].Visible = false;

            if (masterTable.Rows.Count == 0)
            {
                detailsTable.Clear();
                return;
            }

            var ids = masterTable.AsEnumerable()
                                 .Select(r => r["Transaction_ID"].ToString())
                                 .ToList();

            string idList = string.Join(",", ids);

            string qDetails = $@"
                SELECT d.*, i.ItemDescription AS Item, b.BoxTypeDescription AS BoxType
                FROM TransactionDetails d
                LEFT JOIN ItemMasterfile i ON d.ItemID = i.ItemID
                LEFT JOIN BoxTypeMasterfile b ON d.BoxTypeID = b.BoxTypeID
                WHERE d.TransactionID IN ({idList})
                ORDER BY d.TransactionID, d.ItemNo;";

            detailsTable = GetTable(qDetails);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private async void Sync_MainTru_Lan_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Connect LAN to sync latest transactions to main server.",
                "Sync Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

            string serverConn = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
            string localConn = connectionString;

            await ShowLoadingCycle();

            if (!IsServerAvailable(serverConn))
            {
                MessageBox.Show("Cannot connect to main server via LAN. Please check the LAN connection.",
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                SyncAllTransactions(localConn, serverConn);


                MessageBox.Show("Latest transactions synced successfully!",
                    "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sync failed:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void dataGridRecordedDates_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var cellValue = dataGridRecordedDates.Rows[e.RowIndex].Cells["Transaction_ID"].Value;

            if (cellValue == null || cellValue == DBNull.Value)
            {
                MessageBox.Show("This row has no Transaction_ID.", "Invalid Row",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int transactionID = Convert.ToInt32(cellValue);
            DataView dv = new DataView(detailsTable);
            dv.RowFilter = $"TransactionID = {transactionID}";

            Form detailsForm = new Form
            {
                Text = $"Details for Transaction ID {transactionID}",
                Width = 600,
                Height = 400
            };

            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dv,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            detailsForm.Controls.Add(dgv);
            detailsForm.ShowDialog();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //private async void btnDownload_Click(object sender, EventArgs e)
        //{
        //    DialogResult Download = MessageBox.Show(
        //      "If the PC has no LAN connection directly to the Scrap Sale System DB server, this method is very useful.\n\n" +
        //      "Before proceeding with the download, please prepare an authorized flash drive to be used in the Admin and Accounting Scrap Sale System.\n\n" +
        //      "You will be asked to choose where to save the encrypted file.",
        //      "Download Transactions",
        //      MessageBoxButtons.YesNo,
        //      MessageBoxIcon.Information);

        //    if (Download != DialogResult.Yes)
        //        return;

        //    DateTime selectedDate = DateRecord.Value.Date;

        //    if (masterTable.Rows.Count == 0)
        //    {
        //        MessageBox.Show("No transactions loaded for export. Choose a date first.",
        //            "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }

        //    progressBar1.Value = 0;
        //    progressBar1.Visible = true;

        //    StringBuilder sqlDump = new StringBuilder();
        //    var param = new Dictionary<string, object>() { { "@d", selectedDate } };

        //    sqlDump.AppendLine("-- Scrap Export File");
        //    sqlDump.AppendLine("-- Generated: " + DateTime.Now);
        //    sqlDump.AppendLine("-- Selected Transaction Date: " + selectedDate.ToShortDateString());
        //    sqlDump.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
        //    sqlDump.AppendLine();

        //    void AdvanceProgress(int step)
        //    {
        //        progressBar1.Value = Math.Min(progressBar1.Value + step, 100);
        //        progressBar1.Refresh();
        //    }

        //    // --- CustomerTypeMasterfile -----------------------------------------------------------------------------------
        //    var qCustomerTypes = @"SELECT DISTINCT ct.* 
        //                            FROM CustomerTypeMasterfile ct
        //                            JOIN CustomerMasterfile c ON ct.CustomerType_ID = c.CustomerTypeID
        //                            JOIN TransactionMasterfile m ON c.Customer_ID = m.CustomerID
        //                            WHERE m.TransactionDate=@d";
        //    var customerTypes = GetTable(qCustomerTypes, param);
        //    sqlDump.AppendLine("-- CustomerTypeMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("CustomerTypeMasterfile", customerTypes));
        //    AdvanceProgress(10);

        //    // --- CustomerMasterfile ---------------------------------------------------------------------------------------
        //    var qCustomers = @"SELECT DISTINCT c.* 
        //                              FROM CustomerMasterfile c
        //                              JOIN TransactionMasterfile m ON c.Customer_ID = m.CustomerID
        //                              WHERE m.TransactionDate=@d";
        //    var customers = GetTable(qCustomers, param);
        //    sqlDump.AppendLine("-- CustomerMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("CustomerMasterfile", customers));
        //    AdvanceProgress(10);

        //    // --- AccessTypeMasterfile -------------------------------------------------------------------------------------
        //    var qAccessTypes = @"SELECT DISTINCT at.* 
        //                          FROM AccessTypeMasterfile at
        //                          JOIN AdminStaffMasterfile a ON at.AccessType_ID = a.AccessTypeID
        //                          JOIN TransactionMasterfile m ON a.AdminStaff_ID = m.AdminStaffID
        //                          WHERE m.TransactionDate=@d";
        //    var accessTypes = GetTable(qAccessTypes, param);
        //    sqlDump.AppendLine("-- AccessTypeMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("AccessTypeMasterfile", accessTypes));
        //    AdvanceProgress(10);

        //    // --- AdminStaffMasterfile -------------------------------------------------------------------------------------
        //    var qAdmins = @"SELECT DISTINCT a.* 
        //                   FROM AdminStaffMasterfile a
        //                   JOIN TransactionMasterfile m ON a.AdminStaff_ID = m.AdminStaffID
        //                   WHERE m.TransactionDate=@d";
        //    var admins = GetTable(qAdmins, param);
        //    sqlDump.AppendLine("-- AdminStaffMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("AdminStaffMasterfile", admins));
        //    AdvanceProgress(10);

        //    // --- CancelledTransaction -------------------------------------------------------------------------------------
        //    var qCancelled = @"SELECT DISTINCT ct.* 
        //                   FROM CancelledTransaction ct
        //                   JOIN TransactionMasterfile m ON ct.TransactionID = m.Transaction_ID
        //                   WHERE m.TransactionDate=@d";
        //    var cancelled = GetTable(qCancelled, param);
        //    sqlDump.AppendLine("-- CancelledTransaction Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("CancelledTransaction", cancelled));
        //    AdvanceProgress(10);

        //    // --- TransactionMasterfile ------------------------------------------------------------------------------------
        //    sqlDump.AppendLine("-- TransactionMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("TransactionMasterfile", masterTable));
        //    AdvanceProgress(10);

        //    // --- TransactionDetails ---------------------------------------------------------------------------------------
        //    sqlDump.AppendLine("-- TransactionDetails Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("TransactionDetails", detailsTable));
        //    AdvanceProgress(10);

        //    // --- ItemMasterfile -------------------------------------------------------------------------------------------
        //    var qItems = @"SELECT DISTINCT i.* 
        //                 FROM ItemMasterfile i
        //                 JOIN TransactionDetails d ON i.ItemID = d.ItemID
        //                 JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
        //                 WHERE m.TransactionDate=@d";
        //    var items = GetTable(qItems, param);
        //    sqlDump.AppendLine("-- ItemMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("ItemMasterfile", items));
        //    AdvanceProgress(10);

        //    // --- CategoryMasterfile ---------------------------------------------------------------------------------------
        //    var qCategories = @"SELECT DISTINCT cat.* 
        //                    FROM CategoryMasterfile cat
        //                    JOIN ItemMasterfile i ON cat.CategoryID = i.CategoryID
        //                    JOIN TransactionDetails d ON i.ItemID = d.ItemID
        //                    JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
        //                    WHERE m.TransactionDate=@d";
        //    var categories = GetTable(qCategories, param);
        //    sqlDump.AppendLine("-- CategoryMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("CategoryMasterfile", categories));
        //    AdvanceProgress(10);

        //    // --- BoxTypeMasterfile ----------------------------------------------------------------------------------------
        //    var qBoxes = @"SELECT DISTINCT b.* 
        //                    FROM BoxTypeMasterfile b
        //                    JOIN TransactionDetails d ON b.BoxTypeID = d.BoxTypeID
        //                    JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
        //                    WHERE m.TransactionDate=@d";
        //    var boxes = GetTable(qBoxes, param);
        //    sqlDump.AppendLine("-- BoxTypeMasterfile Data");
        //    sqlDump.AppendLine(ConvertTableToSQL("BoxTypeMasterfile", boxes));
        //    AdvanceProgress(10);

        //    sqlDump.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");

        //    for (int i = progressBar1.Value; i <= 100; i++)
        //    {
        //        progressBar1.Value = i;
        //        await Task.Delay(1000);
        //    }

        //    using (SaveFileDialog sfd = new SaveFileDialog())
        //    {
        //        sfd.Title = "Save Encrypted Scrap Export";
        //        sfd.Filter = "Scrap Export (*.scrapenc)|*.scrapenc";
        //        sfd.FileName = $"Scrap_Export_{selectedDate:yyyyMMdd}.scrapenc";

        //        if (sfd.ShowDialog() == DialogResult.OK)
        //        {
        //            string encryptedPath = sfd.FileName;

        //            byte[] encryptedData = EncryptAES(sqlDump.ToString());
        //            File.WriteAllBytes(encryptedPath, encryptedData);

        //            MessageBox.Show(
        //                "Encrypted export completed successfully:\n" + encryptedPath +
        //                "\n\nThis file cannot be opened or edited outside the system.",
        //                "Export Complete",
        //                MessageBoxButtons.OK,
        //                MessageBoxIcon.Information
        //            );
        //        }
        //    }

        //    progressBar1.Visible = false;
        //}
        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (masterTable.Rows.Count == 0)
            {
                MessageBox.Show("No transactions loaded for export. Choose a date first.",
                    "No Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult downloadConfirm = MessageBox.Show(
                "This will export the selected transactions in an encrypted file.\n\n" +
                "Ensure you have an authorized flash drive ready if needed.",
                "Download Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (downloadConfirm != DialogResult.Yes)
                return;

            progressBar1.Value = 0;
            progressBar1.Visible = true;

            DateTime selectedDate = DateRecord.Value.Date;

            StringBuilder sqlDump = new StringBuilder();
            sqlDump.AppendLine("-- Scrap Export File");
            sqlDump.AppendLine("-- Generated: " + DateTime.Now);
            sqlDump.AppendLine("-- Selected Transaction Date: " + selectedDate.ToShortDateString());
            sqlDump.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
            sqlDump.AppendLine();

            void AdvanceProgress(int step)
            {
                progressBar1.Value = Math.Min(progressBar1.Value + step, 100);
                progressBar1.Refresh();
            }

            // --- Export Lookup Tables for only transactions in masterTable ---
            var customerTypes = masterTable.AsEnumerable()
                .Select(r => r["CustomerType"])
                .Distinct()
                .Where(ct => ct != DBNull.Value && ct != null)
                .ToList();

            if (customerTypes.Count > 0)
            {
                var qCustomerTypes = @"SELECT DISTINCT ct.* 
                               FROM CustomerTypeMasterfile ct
                               JOIN CustomerMasterfile c ON ct.CustomerType_ID = c.CustomerTypeID
                               JOIN TransactionMasterfile m ON c.Customer_ID = m.CustomerID
                               WHERE m.TransactionDate=@d";
                var customerTypeTable = GetTable(qCustomerTypes, new Dictionary<string, object> { { "@d", selectedDate } });
                sqlDump.AppendLine("-- CustomerTypeMasterfile Data");
                sqlDump.AppendLine(ConvertTableToSQL("CustomerTypeMasterfile", customerTypeTable));
                AdvanceProgress(10);
            }

            var qCustomers = @"SELECT DISTINCT c.* 
                       FROM CustomerMasterfile c
                       JOIN TransactionMasterfile m ON c.Customer_ID = m.CustomerID
                       WHERE m.TransactionDate=@d";
            var customerTable = GetTable(qCustomers, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- CustomerMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("CustomerMasterfile", customerTable));
            AdvanceProgress(10);

            var qAccessTypes = @"SELECT DISTINCT at.* 
                         FROM AccessTypeMasterfile at
                         JOIN AdminStaffMasterfile a ON at.AccessType_ID = a.AccessTypeID
                         JOIN TransactionMasterfile m ON a.AdminStaff_ID = m.AdminStaffID
                         WHERE m.TransactionDate=@d";
            var accessTypeTable = GetTable(qAccessTypes, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- AccessTypeMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("AccessTypeMasterfile", accessTypeTable));
            AdvanceProgress(10);

            var qAdmins = @"SELECT DISTINCT a.* 
                    FROM AdminStaffMasterfile a
                    JOIN TransactionMasterfile m ON a.AdminStaff_ID = m.AdminStaffID
                    WHERE m.TransactionDate=@d";
            var adminTable = GetTable(qAdmins, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- AdminStaffMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("AdminStaffMasterfile", adminTable));
            AdvanceProgress(10);

            var qCancelled = @"SELECT DISTINCT ct.* 
                       FROM CancelledTransaction ct
                       JOIN TransactionMasterfile m ON ct.TransactionID = m.Transaction_ID
                       WHERE m.TransactionDate=@d";
            var cancelledTable = GetTable(qCancelled, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- CancelledTransaction Data");
            sqlDump.AppendLine(ConvertTableToSQL("CancelledTransaction", cancelledTable));
            AdvanceProgress(10);

            // --- Transactions ---
            sqlDump.AppendLine("-- TransactionMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("TransactionMasterfile", masterTable));
            AdvanceProgress(10);

            sqlDump.AppendLine("-- TransactionDetails Data");
            sqlDump.AppendLine(ConvertTableToSQL("TransactionDetails", detailsTable));
            AdvanceProgress(10);

            // --- Item, Category, BoxType ---
            var qItems = @"SELECT DISTINCT i.* 
                   FROM ItemMasterfile i
                   JOIN TransactionDetails d ON i.ItemID = d.ItemID
                   JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
                   WHERE m.TransactionDate=@d";
            var itemsTable = GetTable(qItems, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- ItemMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("ItemMasterfile", itemsTable));
            AdvanceProgress(10);

            var qCategories = @"SELECT DISTINCT cat.* 
                        FROM CategoryMasterfile cat
                        JOIN ItemMasterfile i ON cat.CategoryID = i.CategoryID
                        JOIN TransactionDetails d ON i.ItemID = d.ItemID
                        JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
                        WHERE m.TransactionDate=@d";
            var categoriesTable = GetTable(qCategories, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- CategoryMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("CategoryMasterfile", categoriesTable));
            AdvanceProgress(10);

            var qBoxes = @"SELECT DISTINCT b.* 
                   FROM BoxTypeMasterfile b
                   JOIN TransactionDetails d ON b.BoxTypeID = d.BoxTypeID
                   JOIN TransactionMasterfile m ON d.TransactionID = m.Transaction_ID
                   WHERE m.TransactionDate=@d";
            var boxesTable = GetTable(qBoxes, new Dictionary<string, object> { { "@d", selectedDate } });
            sqlDump.AppendLine("-- BoxTypeMasterfile Data");
            sqlDump.AppendLine(ConvertTableToSQL("BoxTypeMasterfile", boxesTable));
            AdvanceProgress(10);

            sqlDump.AppendLine("SET FOREIGN_KEY_CHECKS = 1;");

            // --- Save encrypted file ---
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Save Encrypted Scrap Export";
                sfd.Filter = "Scrap Export (*.scrapenc)|*.scrapenc";
                sfd.FileName = $"Scrap_Export_{selectedDate:yyyyMMdd}.scrapenc";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    byte[] encryptedData = EncryptAES(sqlDump.ToString());
                    File.WriteAllBytes(sfd.FileName, encryptedData);

                    MessageBox.Show(
                        "Encrypted export completed successfully:\n" + sfd.FileName +
                        "\n\nThis file cannot be opened or edited outside the system.",
                        "Export Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
            }

            progressBar1.Visible = false;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void DownloadTruExcel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (SelectTransaction.SelectedItem == null)
            {
                MessageBox.Show("Please select a transaction first.");
                return;
            }

            string transactionNo = SelectTransaction.Text;

            string qTrans = @"SELECT m.Transaction_ID, m.TransactionDate, c.CustomerName
                            FROM TransactionMasterfile m
                            LEFT JOIN CustomerMasterfile c ON m.CustomerID = c.Customer_ID
                            WHERE m.`Transaction_No.` = @tno";

            DataTable dtTrans = GetTable(qTrans, new Dictionary<string, object> { { "@tno", transactionNo } });

            if (dtTrans.Rows.Count == 0)
            {
                MessageBox.Show("Transaction not found.");
                return;
            }

            int transId = Convert.ToInt32(dtTrans.Rows[0]["Transaction_ID"]);
            DateTime transDate = Convert.ToDateTime(dtTrans.Rows[0]["TransactionDate"]);
            string customerName = dtTrans.Rows[0]["CustomerName"].ToString();

            string qDetails = @"SELECT 
                              td.Weight, 
                              td.OtherScraps,
                              i.ItemDescription, 
                              cat.CategoryDescription
                          FROM TransactionDetails td
                          LEFT JOIN ItemMasterfile i ON td.ItemID = i.ItemID
                          LEFT JOIN CategoryMasterfile cat ON i.CategoryID = cat.CategoryID
                          WHERE td.TransactionID = @id
                          AND td.IsVoid = 0
                          ORDER BY td.ItemNo";


            DataTable dtDetails = GetTable(qDetails, new Dictionary<string, object> { { "@id", transId } });
            if (dtDetails.Rows.Count == 0)
            {
                MessageBox.Show("No items found for this transaction.");
                return;
            }

            DataTable allItems = GetTable(@"
                            SELECT i.ItemDescription, cat.CategoryDescription
                            FROM ItemMasterfile i
                            JOIN CategoryMasterfile cat ON i.CategoryID = cat.CategoryID
                            ORDER BY cat.CategoryDescription, i.ItemDescription");

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Daily Hauling Summary");

                // ================= HEADER =================
                int headerRow = 2;
                int totalColumns = 8;

                ws.Range(headerRow, 1, headerRow, totalColumns).Merge();
                ws.Cell(headerRow, 1).Value = "Summary of Daily Hauling";
                ws.Cell(headerRow, 1).Style.Font.Bold = true;
                ws.Cell(headerRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Range(headerRow + 1, 1, headerRow + 1, totalColumns).Merge();
                ws.Cell(headerRow + 1, 1).Value = $"Customer Name: {customerName}";
                ws.Cell(headerRow + 1, 1).Style.Font.Bold = true;
                ws.Cell(headerRow + 1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(headerRow + 2, totalColumns).Value = $"Hauling Date: {transDate:yyyy-MM-dd}";
                ws.Cell(headerRow + 2, totalColumns).Style.Font.Bold = true;
                ws.Cell(headerRow + 2, totalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // ================= SOLID WASTE =================
                string[] solidCategories = { "Carton Boxes", "Plastic", "Paper", "Metal" };
                int solidHeaderRow = headerRow + 3;
                ws.Cell(solidHeaderRow, 1).Value = "Solid Waste (Weight in Kgs)";
                ws.Cell(solidHeaderRow, 1).Style.Font.Bold = true;

                int catRow = solidHeaderRow + 1;
                int itemRow = solidHeaderRow + 2;
                int dataRow = solidHeaderRow + 3;
                int col = 1;

                Dictionary<string, int> itemColMap = new Dictionary<string, int>();

                foreach (string cat in solidCategories)
                {
                    var items = allItems.AsEnumerable()
                        .Where(r => r["CategoryDescription"].ToString() == cat)
                        .Select(r => r["ItemDescription"].ToString())
                        .ToList();

                    int startCol = col;
                    foreach (string item in items)
                    {
                        ws.Cell(itemRow, col).Value = item;
                        ws.Cell(itemRow, col).Style.Font.Bold = true;
                        ws.Cell(itemRow, col).Style.Fill.BackgroundColor = XLColor.LightGray;
                        ws.Cell(itemRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        itemColMap[item] = col;
                        col++;
                    }

                    ws.Range(catRow, startCol, catRow, col - 1).Merge();
                    ws.Cell(catRow, startCol).Value = cat;
                    ws.Cell(catRow, startCol).Style.Font.Bold = true;
                    ws.Cell(catRow, startCol).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
                    ws.Cell(catRow, startCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
             
                Dictionary<int, int> columnRowTracker = new Dictionary<int, int>();

                foreach (var colIndex in itemColMap.Values)
                {
                    columnRowTracker[colIndex] = dataRow; 
                }

                foreach (DataRow r in dtDetails.Rows)
                {
                    string item = r["ItemDescription"].ToString();
                    if (itemColMap.ContainsKey(item))
                    {
                        int colIndex = itemColMap[item];
                        int rowToWrite = columnRowTracker[colIndex];

                        ws.Cell(rowToWrite, colIndex).Value = r["Weight"];
                        columnRowTracker[colIndex]++; 
                    }
                }


                //   int solidEndRow = columnRowTracker.Values.Max() - 1;
                int solidEndRow = (columnRowTracker.Values.Any()) ? columnRowTracker.Values.Max() - 1 : dataRow - 1;


                int solidTotalRow = solidEndRow + 1;

                for (int c = 1; c < col; c++)
                {
                    ws.Cell(solidTotalRow, c).FormulaA1 =
                        $"=SUM({ws.Cell(dataRow, c).Address}:{ws.Cell(solidEndRow, c).Address})";
                    ws.Cell(solidTotalRow, c).Style.Font.Bold = true;
                    ws.Cell(solidTotalRow, c).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

              
                int solidKgRow = solidTotalRow + 2;
                ws.Cell(solidKgRow, 1).Value = "Total Kilogram:";
                ws.Cell(solidKgRow, 1).Style.Font.Bold = true;

                string solidKgFormula = string.Join("+",
                    Enumerable.Range(1, col - 1).Select(c => ws.Cell(solidTotalRow, c).Address.ToString()));

                ws.Cell(solidKgRow, 2).FormulaA1 = "=" + solidKgFormula;
                ws.Cell(solidKgRow, 2).Style.Font.Bold = true;

                // ================= WIRES =================
                int wireStartRow = solidKgRow + 3;
                ws.Cell(wireStartRow, 1).Value = "Wires (Weight in Kgs)";
                ws.Cell(wireStartRow, 1).Style.Font.Bold = true;

                var wireItems = allItems.AsEnumerable()
                    .Where(r => r["CategoryDescription"].ToString() == "Wires")
                    .Select(r => r["ItemDescription"].ToString())
                    .ToList();

                Dictionary<string, int> wireColMap = new Dictionary<string, int>();
                for (int i = 0; i < wireItems.Count; i++)
                {
                    ws.Cell(wireStartRow + 1, i + 1).Value = wireItems[i];
                    ws.Cell(wireStartRow + 1, i + 1).Style.Font.Bold = true;
                    ws.Cell(wireStartRow + 1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    ws.Cell(wireStartRow + 1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    wireColMap[wireItems[i]] = i + 1;
                }

                
                Dictionary<int, int> wireColumnRowTracker = new Dictionary<int, int>();

                foreach (var colIndex in wireColMap.Values)
                {
                    wireColumnRowTracker[colIndex] = wireStartRow + 2;
                }

                foreach (DataRow r in dtDetails.Select("CategoryDescription = 'Wires'"))
                {
                    string item = r["ItemDescription"].ToString();
                    if (wireColMap.ContainsKey(item))
                    {
                        int colIndex = wireColMap[item];
                        int rowToWrite = wireColumnRowTracker[colIndex];

                        ws.Cell(rowToWrite, colIndex).Value = r["Weight"];
                        wireColumnRowTracker[colIndex]++;
                    }
                }


                //   int wireEndRow = wireColumnRowTracker.Values.Max() - 1;
                int wireEndRow = (wireColumnRowTracker.Values.Any()) ? wireColumnRowTracker.Values.Max() - 1 : wireStartRow + 1;


                int wireTotalRow = wireEndRow + 1;

                for (int c = 1; c <= wireItems.Count; c++)
                {
                    ws.Cell(wireTotalRow, c).FormulaA1 =
                        $"=SUM({ws.Cell(wireStartRow + 2, c).Address}:{ws.Cell(wireEndRow, c).Address})";
                    ws.Cell(wireTotalRow, c).Style.Font.Bold = true;
                    ws.Cell(wireTotalRow, c).Style.Fill.BackgroundColor = XLColor.LightYellow;
                }

               
                int wireKgRow = wireTotalRow + 2;
                ws.Cell(wireKgRow, 1).Value = "Total Kilogram:";
                ws.Cell(wireKgRow, 1).Style.Font.Bold = true;

                string wireKgFormula = string.Join("+",
                    Enumerable.Range(1, wireItems.Count).Select(c => ws.Cell(wireTotalRow, c).Address.ToString()));

                ws.Cell(wireKgRow, 2).FormulaA1 = "=" + wireKgFormula;
                ws.Cell(wireKgRow, 2).Style.Font.Bold = true;

        
                var solidTableRange = ws.Range(catRow, 1, solidTotalRow, col - 1);
                solidTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                solidTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns(1, col - 1).Width = 18; 
                ws.Rows(dataRow, solidTotalRow).Height = 20; 

          
                var wireTableRange = ws.Range(wireStartRow + 1, 1, wireTotalRow, wireItems.Count);
                wireTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                wireTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                ws.Columns(1, wireItems.Count).Width = 18;
                ws.Rows(wireStartRow + 2, wireTotalRow).Height = 20;

                // ================= OTHER SCRAPS =================
                int otherStartRow = wireKgRow + 3;
                ws.Cell(otherStartRow, 1).Value = "Other Scraps (Weight in Kgs)";
                ws.Cell(otherStartRow, 1).Style.Font.Bold = true;

                var otherItems = dtDetails.AsEnumerable()
                    .Where(r => !string.IsNullOrEmpty(r["OtherScraps"].ToString()))
                    .Select(r => r["OtherScraps"].ToString())
                    .Distinct()
                    .ToList();

                if (otherItems.Count > 0)
                {
                    Dictionary<string, int> otherColMap = new Dictionary<string, int>();

                    for (int i = 0; i < otherItems.Count; i++)
                    {
                        ws.Cell(otherStartRow + 1, i + 1).Value = otherItems[i];
                        ws.Cell(otherStartRow + 1, i + 1).Style.Font.Bold = true;
                        ws.Cell(otherStartRow + 1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                        ws.Cell(otherStartRow + 1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        otherColMap[otherItems[i]] = i + 1;
                    }

                    Dictionary<int, int> otherColumnRowTracker = new Dictionary<int, int>();
                    foreach (var colIndex in otherColMap.Values)
                    {
                        otherColumnRowTracker[colIndex] = otherStartRow + 2;
                    }

                    foreach (DataRow r in dtDetails.Rows)
                    {
                        string otherScrap = r["OtherScraps"].ToString();
                        if (!string.IsNullOrEmpty(otherScrap) && otherColMap.ContainsKey(otherScrap))
                        {
                            int colIndex = otherColMap[otherScrap];
                            int rowToWrite = otherColumnRowTracker[colIndex];

                            ws.Cell(rowToWrite, colIndex).Value = r["Weight"];
                            otherColumnRowTracker[colIndex]++;
                        }
                    }

                    int otherEndRow = (otherColumnRowTracker.Values.Any())
                        ? otherColumnRowTracker.Values.Max() - 1
                        : otherStartRow + 1;

                    int otherTotalRow = otherEndRow + 1;

                    for (int c = 1; c <= otherItems.Count; c++)
                    {
                        ws.Cell(otherTotalRow, c).FormulaA1 =
                            $"=SUM({ws.Cell(otherStartRow + 2, c).Address}:{ws.Cell(otherEndRow, c).Address})";
                        ws.Cell(otherTotalRow, c).Style.Font.Bold = true;
                        ws.Cell(otherTotalRow, c).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    }

                    int otherKgRow = otherTotalRow + 2;
                    ws.Cell(otherKgRow, 1).Value = "Total Kilogram:";
                    ws.Cell(otherKgRow, 1).Style.Font.Bold = true;

                    string otherKgFormula = string.Join("+",
                        Enumerable.Range(1, otherItems.Count).Select(c => ws.Cell(otherTotalRow, c).Address.ToString()));

                    ws.Cell(otherKgRow, 2).FormulaA1 = "=" + otherKgFormula;
                    ws.Cell(otherKgRow, 2).Style.Font.Bold = true;

                    var otherTableRange = ws.Range(otherStartRow + 1, 1, otherTotalRow, otherItems.Count);
                    otherTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    otherTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    ws.Columns(1, otherItems.Count).Width = 18;
                    ws.Rows(otherStartRow + 2, otherTotalRow).Height = 20;
                }
                else
                {
                    ws.Cell(otherStartRow + 1, 1).Value = "No Other Scraps found.";
                    ws.Cell(otherStartRow + 1, 1).Style.Font.Italic = true;
                }

                // ===================== SAVE FILE =====================
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    sfd.FileName = $"Summary_{transactionNo}.xlsx";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        string path = sfd.FileName;

                        if (File.Exists(path))
                        {
                            try
                            {
                                File.Delete(path);
                            }
                            catch (IOException)
                            {
                                MessageBox.Show(
                                    "Please close the Excel file before exporting.",
                                    "File in Use",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                                return;
                            }
                        }

                        workbook.SaveAs(path);
                        MessageBox.Show("Excel file exported successfully!");
                    }

                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void progressBar1_Click(object sender, EventArgs e) { }
        private void dataGridRecordedDates_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
        private void SelectTransaction_SelectedIndexChanged(object sender, EventArgs e){ }
    }
}
