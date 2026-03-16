using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace Digital_Weighing_Scale
{
    public partial class isVoid : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        //  string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        //  string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=masterx;database=Scraps";


        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
        private TransactionScreen intoTheTransaction;


        public isVoid(TransactionScreen jumpBackToTransScreen)
        {
            InitializeComponent();

            intoTheTransaction = jumpBackToTransScreen;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.KeyDown += BackToTransactionScreen;
            this.KeyDown += Ok_IsVoid;
            this.KeyDown += AdminUser;
            this.KeyDown += PSWD;
            this.KeyDown += SelectItem;
            this.KeyDown += dataGridView1ScrollControl;


            textBox2.Text = "In-charge username";
            textBox2.ForeColor = Color.Gray;
            textBox2.Enter += Remove_AdminUserPlaceHolder;
            textBox2.Leave += AddAdminUserPlaceHolder;

            textBox3.Text = "Password";
            textBox3.ForeColor = Color.Gray;
            textBox3.Enter += RemovePasswordPlaceHolder;
            textBox3.Leave += AddPasswordPlaceHolder;

            dataGridView1.ReadOnly = true;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Backup function incase there are errors would occur
        /*public void LoadItemsFromTransaction()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.ColumnCount = 3; 
            dataGridView1.Columns[0].Name = "No.";
            dataGridView1.Columns[1].Name = "Item Description";
            dataGridView1.Columns[2].Name = "Weight (KGS)";

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 16, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;

            int no = 1; 
            foreach (DataGridViewRow row in intoTheTransaction.TransactionGrid.Rows)
            {
                if (row.IsNewRow) continue;
                string itemDesc = row.Cells["Item Description"].Value?.ToString() ?? "";
                string weight = row.Cells["Weight (KGS)"].Value?.ToString() ?? "";
                dataGridView1.Rows.Add(no, itemDesc, weight);
                no++;
            }
        }
        */
        public void LoadItemsFromTransaction()
        {
            dataGridView1.Rows.Clear();
            dataGridView1.ColumnCount = 3;
            dataGridView1.Columns[0].Name = "No.";
            dataGridView1.Columns[1].Name = "Item Description";
            dataGridView1.Columns[2].Name = "NET Weight (KGS) - Box";

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 16, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;

            int no = 1;
            foreach (DataGridViewRow row in intoTheTransaction.TransactionGrid.Rows)
            {
                if (row.IsNewRow) continue;

             
                bool isVoided = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    Font cellFont = cell.Style?.Font ?? cell.InheritedStyle?.Font;
                    if (cellFont != null && cellFont.Strikeout)
                    {
                        isVoided = true;
                        break;
                    }
                }

                if (isVoided) continue; 

                string itemDesc = row.Cells["Item Description"].Value?.ToString() ?? "";
                string weight = row.Cells["NET Weight (KGS) - Box"].Value?.ToString() ?? "";
                dataGridView1.Rows.Add(no, itemDesc, weight);
                no++;
            }
        }



        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void dataGridView1ScrollControl(object sender, KeyEventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
                return;

            int firstVisible = dataGridView1.FirstDisplayedScrollingRowIndex;

            if (e.KeyCode == Keys.Down)
            {
                if (firstVisible < dataGridView1.Rows.Count - 1)
                    dataGridView1.FirstDisplayedScrollingRowIndex = firstVisible + 1;

                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                if (firstVisible > 0)
                    dataGridView1.FirstDisplayedScrollingRowIndex = firstVisible - 1;

                e.Handled = true;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //=============================This is for keyboard trigger=============================
        private void BackToTransactionScreen(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                intoTheTransaction.Show();
                this.Close();
            }
        }
        
        //--------------------------------------------------------
        private void Ok_IsVoid(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {

                button1_Click(sender, e);
               
            
            
            }
        }
        //--------------------------------------------------------
        private void AdminUser (object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F6)
            {
                textBox2.Focus();
                e.SuppressKeyPress = true;

            }
        }
        //--------------------------------------------------------
        private void SelectItem(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F5)
            {
                textBox1.Focus();
                e.SuppressKeyPress = true;
            }
        }
        //--------------------------------------------------------
        private void PSWD (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F7)
            {
                textBox3.Focus();
                e.SuppressKeyPress = true;
            }
        }

        //============================End of Trigger Keyboard logic=============================
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //======================================================================================
        private void Remove_AdminUserPlaceHolder(object sender, EventArgs e)
        {
            if(textBox2.Text == "In-charge username")
            {
                textBox2.Text = "";
                textBox2.ForeColor = Color.Black;
            }
        }
        //----------------------------------------------------------------------------------------------------------
        private void AddAdminUserPlaceHolder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                textBox2.Text = "In-charge username";
                textBox2.ForeColor = Color.Gray;
            }
        }
        //----------------------------------------------------------------------------------------------------------
        private void RemovePasswordPlaceHolder(object sender, EventArgs e)
        {
            if(textBox3.Text == "Password")
            {
                textBox3.Text = "";
                textBox3.ForeColor = Color.Black;
                textBox3.UseSystemPasswordChar = true;
            }
        }
        //----------------------------------------------------------------------------------------------------------
        private void AddPasswordPlaceHolder(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox3.Text))
            {
                textBox3.UseSystemPasswordChar = false;
                textBox3.Text = "Password";
                textBox3.ForeColor = Color.Gray;
            }
        }
        //======================================================================================
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Cancel_button2_Click(object sender, EventArgs e)
        {
            intoTheTransaction.Show();
            this.Close();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
         private void button1_Click(object sender, EventArgs e)
         {
             string username = textBox2.Text.Trim();
             string password = textBox3.Text.Trim();

             if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
             {
                 MessageBox.Show("Please enter admin username and password.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
             }

             using (MySqlConnection connection = new MySqlConnection(connectionString))
             {
                 connection.Open();

                 // Verify admin credentials
                string checkAdmin = @"SELECT AdminStaff_ID 
                      FROM AdminStaffMasterfile 
                      WHERE BINARY AdminStaffName = @Username 
                      AND BINARY `Password` = @Password
                      LIMIT 1;";
                int adminID = -1;
                 using (MySqlCommand command = new MySqlCommand(checkAdmin, connection))
                 {
                     command.Parameters.AddWithValue("@Username", username);
                     command.Parameters.AddWithValue("@Password", password);

                     object result = command.ExecuteScalar();
                     if (result == null)
                     {
                         MessageBox.Show("Invalid admin credentials.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                         return;
                     }

                     adminID = Convert.ToInt32(result);
                 }

                 // Parse item numbers from textBox1
                 string input = textBox1.Text?.Trim() ?? "";
                 if (string.IsNullOrWhiteSpace(input))
                 {
                     MessageBox.Show("Please enter the item No. (e.g. 2, 3-5, 1,4).", "Input required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                     return;
                 }

                 var numbers = ParseNumberList(input);
                 if (numbers.Count == 0)
                 {
                     MessageBox.Show("No valid numbers found in input. Use numbers, commas, or ranges (e.g. 2,4-6).", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     return;
                 }

                 var skippedItems = new List<int>(); 

                 foreach (int itemNo in numbers)
                 {
                     try
                     {
                         // Check if item is already voided
                         string checkQuery = @"SELECT IsVoid FROM TransactionDetails
                                       WHERE TransactionID = @TransactionID AND ItemNo = @ItemNo;";
                         using (MySqlCommand cmdCheck = new MySqlCommand(checkQuery, connection))
                         {
                             cmdCheck.Parameters.AddWithValue("@TransactionID", intoTheTransaction.CurrentTransactionID);
                             cmdCheck.Parameters.AddWithValue("@ItemNo", itemNo);

                             object isVoidObj = cmdCheck.ExecuteScalar();
                             if (isVoidObj != null && Convert.ToInt32(isVoidObj) == 1)
                             {
                                 skippedItems.Add(itemNo); // Already voided, skip
                                 continue;
                             }
                         }

                         // Update TransactionDetails table to mark as void===================================================
                         string updateQuery = @"UPDATE TransactionDetails SET IsVoid = 1 WHERE TransactionID = @TransactionID AND ItemNo = @ItemNo;";

                         using (MySqlCommand cmdUpdate = new MySqlCommand(updateQuery, connection))
                         {
                             cmdUpdate.Parameters.AddWithValue("@TransactionID", intoTheTransaction.CurrentTransactionID);
                             cmdUpdate.Parameters.AddWithValue("@ItemNo", itemNo);
                             cmdUpdate.ExecuteNonQuery();
                         }
                         //===================================================================================================


                         intoTheTransaction.ApplyVoidEffect(itemNo);
                     }
                     catch (Exception ex)
                     {
                         System.Diagnostics.Debug.WriteLine($"ApplyVoidEffect({itemNo}) failed: {ex.Message}");
                     }
                 }

                 if (skippedItems.Count > 0)
                 {
                     MessageBox.Show($"Some items were already voided and skipped: {string.Join(", ", skippedItems)}",
                                     "Skipped Items", MessageBoxButtons.OK, MessageBoxIcon.Information);
                 }
                 else
                 {
                     MessageBox.Show("Selected item(s) have been excluded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                 }

                 this.Close();
                // After voiding items
                intoTheTransaction.UpdateTotalItems();

            }
        } 
      
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void isVoid_Load(object sender, EventArgs e)
        {
            LoadItemsFromTransaction();
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.RowTemplate.Height = 40;

            // ===== Font and Style ====================================================================
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 10, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Blue;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // ===== Header Style ======================================================================
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkBlue;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.EnableHeadersVisualStyles = false;

           
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // parse input like "2", "2,4,7", "3-5" into a list of ints=======================================
        private List<int> ParseNumberList(string input)
{
    var result = new List<int>();
    if (string.IsNullOrWhiteSpace(input)) return result;

    var parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var partRaw in parts)
    {
        var part = partRaw.Trim();
        if (part.Contains("-"))
        {
            var bounds = part.Split('-');
            if (bounds.Length == 2
                && int.TryParse(bounds[0].Trim(), out int start)
                && int.TryParse(bounds[1].Trim(), out int end))
            {
                if (end < start) (start, end) = (end, start);
                for (int i = start; i <= end; i++) result.Add(i);
            }
        }
                else
                {
                    if (int.TryParse(part, out int n)) result.Add(n);
                }
    }

    // remove duplicates 
    return result.Distinct().OrderBy(x => x).ToList();
}

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void textBox3_TextChanged(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e){ }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
