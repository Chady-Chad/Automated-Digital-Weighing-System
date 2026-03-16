using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace Digital_Weighing_Scale
{

    public partial class Cancel_Transaction : Form
         //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    {
        public bool IsApproved { get; private set; } = false;

        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        // string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        //  string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=masterx;database=Scraps";


        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        private Main_Menu mainMenuForm;
        private TransactionScreen trans;
        private string CurrentTransactionNumber;

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public Cancel_Transaction(TransactionScreen goBackToTrans, string transactionNumber, Main_Menu main_Menu)
        {
            InitializeComponent();

            trans = goBackToTrans;
            CurrentTransactionNumber = transactionNumber;
            mainMenuForm = main_Menu; 
            richTextBox1.ReadOnly = true;
            this.KeyPreview = true;
            this.KeyDown += BackToTransaction;
            this.KeyDown += Ok_TransactionCancellation;
            this.KeyDown += AdminUser;
            this.KeyDown += PSWD;
            this.KeyDown += ReasonBox;

            this.StartPosition = FormStartPosition.CenterScreen;

            // placeholder setup (unchanged)
            textBox1.Text = "In-charge username"; 
            textBox1.ForeColor = Color.Gray;
            textBox1.Enter += RemovePlaceholder_Username;
            textBox1.Leave += AddPlaceholder_Username;

            textBox2.Text = "Password";
            textBox2.ForeColor = Color.Gray;
            textBox2.UseSystemPasswordChar = false;
            textBox2.Enter += RemovePlaceholder_Password;
            textBox2.Leave += AddPlaceholder_Password;

            richTextBox2.Text = "Reason for Cancellation:";
            richTextBox2.ForeColor = Color.Gray;
            richTextBox2.Enter += RemovePlaceholder_Reason;
            richTextBox2.Leave += AddPlaceholder_Reason;

            
        }
        
     
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Username placeholder
        private void RemovePlaceholder_Username(object sender, EventArgs e)
        {
            if (textBox1.Text == "In-charge username")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void AddPlaceholder_Username(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                textBox1.Text = "In-charge username";
                textBox1.ForeColor = Color.Gray;
            }
        }

        // Password placeholder
        private void RemovePlaceholder_Password(object sender, EventArgs e)
        {
            if (textBox2.Text == "Password")
            {
                textBox2.Text = "";
                textBox2.ForeColor = Color.Black;
                textBox2.UseSystemPasswordChar = true;
            }
        }

        private void AddPlaceholder_Password(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                textBox2.UseSystemPasswordChar = false;
                textBox2.Text = "Password";
                textBox2.ForeColor = Color.Gray;
            }
        }

        // Reason placeholder
        private void RemovePlaceholder_Reason(object sender, EventArgs e)
        {
            if (richTextBox2.Text == "Reason for Cancellation:")
            {
                richTextBox2.Text = "";
                richTextBox2.ForeColor = Color.Black;
            }
        }

        private void AddPlaceholder_Reason(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox2.Text))
            {
                richTextBox2.Text = "Reason for Cancellation:";
                richTextBox2.ForeColor = Color.Gray;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void BackToTransaction(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                trans.Show();
                this.Close();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Ok_TransactionCancellation(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                button1.PerformClick();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void AdminUser(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F6)
            {
                textBox1.Focus();
                e.SuppressKeyPress = true;
            }
        }
        private void PSWD(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                textBox2.Focus();
                e.SuppressKeyPress = true;
            }
        }
        private void ReasonBox(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                richTextBox2.Focus();
                e.SuppressKeyPress = true;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {

          richTextBox1.Text = $"Transaction No: {CurrentTransactionNumber}";
          HideCaret(richTextBox1.Handle); // hide caret
            richTextBox1.ReadOnly = true; richTextBox1.Font = new Font("Arial", 28, FontStyle.Bold); richTextBox1.ForeColor = Color.Blue;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button2_Click(object sender, EventArgs e)
        {
            trans.Show();
            this.Close();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // The function button1_Click is to approve the cancellation order==============
        private void button1_Click(object sender, EventArgs e)
        {
            string reason = richTextBox2.Text.Trim();
            string username = textBox1.Text.Trim();
            string password = textBox2.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason) || reason == "Reason for Cancellation:")
            {
                MessageBox.Show("Please enter a reason for cancellation.", "Empty Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter admin username and password.", "Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Check admin credentials=============================================================================
                   
                    string checkAdmin = @"SELECT AdminStaff_ID 
                      FROM AdminStaffMasterfile 
                      WHERE BINARY AdminStaffName = @Username 
                      AND BINARY `Password` = @Password
                      LIMIT 1;";

                    int adminID = -1;
                    using (MySqlCommand cmd = new MySqlCommand(checkAdmin, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);

                        object result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("Invalid admin credentials.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        adminID = Convert.ToInt32(result);
                    }

                    //Check if Transaction_No. exists, if not insert it==================================================
                    //backup.if opening this, make sure to remove the label here:  richTextBox1.Text = $"Transaction No: {CurrentTransactionNumber}";. Leave only CurrentTransactionNumber
                    // string transactionNo = richTextBox1.Text.Trim();
                    string transactionNo = CurrentTransactionNumber; 

                    string getTransactionIDQuery = @"SELECT Transaction_ID 
                                             FROM TransactionMasterfile
                                             WHERE `Transaction_No.` = @TransactionNo
                                             LIMIT 1;";
                    int transactionID = -1;
                    using (MySqlCommand cmd = new MySqlCommand(getTransactionIDQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionNo", transactionNo);
                        object result = cmd.ExecuteScalar();

                        if (result == null)
                        {
                            // Transaction not found. Insert it into TransactionMasterfile first=========================
                            string insertTransaction = @"INSERT INTO TransactionMasterfile
                                                 (`Transaction_No.`, TransactionDate)
                                                 VALUES (@TransactionNo, @TransactionDate);";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertTransaction, conn))
                            {
                                insertCmd.Parameters.AddWithValue("@TransactionNo", transactionNo);
                                insertCmd.Parameters.AddWithValue("@TransactionDate", DateTime.Now.Date);
                                insertCmd.ExecuteNonQuery();
                                transactionID = (int)insertCmd.LastInsertedId;
                            }
                        }
                        else
                        {
                            transactionID = Convert.ToInt32(result);
                        }
                    }

                    //Insert into CancelledTransaction===================================================================
                    string insertCancelled = @"INSERT INTO CancelledTransaction
                                       (TransactionID, Reason, DateTimeCancelled, CancelledByID)
                                       VALUES (@TransactionID, @Reason, @DateTimeCancelled, @CancelledByID);";
                    using (MySqlCommand cmd = new MySqlCommand(insertCancelled, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionID", transactionID);
                        cmd.Parameters.AddWithValue("@Reason", reason);
                        cmd.Parameters.AddWithValue("@DateTimeCancelled", DateTime.Now);
                        cmd.Parameters.AddWithValue("@CancelledByID", adminID);

                        cmd.ExecuteNonQuery();
                    }

                    // =========== Update IsCancel field ================================================================
                    string updateIsCancel = @"UPDATE TransactionMasterfile
                                      SET IsCancel = 1
                                      WHERE Transaction_ID = @TransactionID;";
                    using (MySqlCommand cmd = new MySqlCommand(updateIsCancel, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionID", transactionID);
                        cmd.ExecuteNonQuery();
                    }
                    // =================================================================================================

                    MessageBox.Show("Transaction successfully cancelled.", "Cancelled Successfully", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    trans.allowClose = true;
                    trans.Close();
                        this.Close();
                        mainMenuForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Isolate these voids for future functions .

        private void ReasonBox_richTextBox2_TextChanged(object sender, EventArgs e) { }
        private void Transaction_richTextBox1_TextChanged(object sender, EventArgs e){ }
        private void User_textBox1_TextChanged(object sender, EventArgs e){ }
        private void Password_textBox2_TextChanged(object sender, EventArgs e) { }
    

    }
}
