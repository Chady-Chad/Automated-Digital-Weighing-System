using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Configuration;
using System.Xml.Linq;
using MySql.Data.MySqlClient;

namespace Digital_Weighing_Scale
{
    public partial class New_Transaction : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        // string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=masterx;database=Scraps";


        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
        private Main_Menu mainMenu;
        private ToolTip RefreshDataSetting = new ToolTip();
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Initialize key components.
        public New_Transaction(Main_Menu menu)
        {
            InitializeComponent();


            mainMenu = menu;
            this.KeyPreview = true;
            this.KeyDown += BackToTheMainMenu;
            // this.FormClosed += New_Transaction_FormClosed;
            this.KeyDown += RefreshData;
            this.KeyDown += selectCompanyRadioButton;
            this.KeyDown += selectEmployeeRadioButton;
            this.KeyDown += selectAdminNameCombo;
            this.KeyDown += SelectSecuText;
            this.TopLevel = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
       

            richTextBox1.ReadOnly = true;
            richTextBox1.Font = new Font("Arial", 28, FontStyle.Bold);
            HideCaret(richTextBox1.Handle);

            comboBox1.Enabled = false;
            comboBox1.BackColor = Color.LightGray;
            comboBox3.Visible = false;

            textBox1.Enabled = false;
            textBox1.BackColor = Color.LightGray;

            comboBox2.Enabled = false;
            comboBox2.BackColor = Color.LightGray;

            comboBox1.SelectedIndexChanged += Hauler_comboBox1_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += Employee_comboBox3_SelectedIndexChanged;

            radioButton1.CheckedChanged += Company_radioButton1_CheckedChanged;
            radioButton2.CheckedChanged += Employee_radioButton2_CheckedChanged;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
   
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Load customers dynamically.
        private void LoadCustomers(int customerTypeID, ComboBox comboBox)
        {
            comboBox.Items.Clear();
            comboBox.Items.Add(customerTypeID == 2 ? "Select Company Name" : "Select Employee Name");

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT CustomerName FROM CustomerMasterfile WHERE CustomerTypeID = @CustomerTypeID ORDER BY CustomerName ASC;";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerTypeID", customerTypeID);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            comboBox.Items.Add(reader["CustomerName"].ToString());
                    }
                }
            }

            comboBox.SelectedIndex = 0;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Show address based on selected customer.
        private void ShowCustomerAddress(string customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
            {
                richTextBox2.Clear();
                return;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT CustomerAddress FROM CustomerMasterfile WHERE CustomerName = @CustomerName LIMIT 1;";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CustomerName", customerName);
                    object result = cmd.ExecuteScalar();
                    richTextBox2.Text = result?.ToString() ?? "";
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Radio button events
        private void Company_radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                comboBox1.Visible = true;
                comboBox1.Enabled = true;
                comboBox1.BackColor = Color.White;
                comboBox3.Visible = false;
                richTextBox2.Clear();
                LoadCustomers(2, comboBox1);

                comboBox2.Enabled = true;
                comboBox2.BackColor = Color.White;
            }

        }
        //===================================================================================
        private void Employee_radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                comboBox1.Visible = false;
                comboBox3.Visible = true;
                richTextBox2.Clear();
                LoadCustomers(1, comboBox3);

                comboBox2.Enabled = true;
                comboBox2.BackColor = Color.White;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // ComboBox address updates
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCustomerAddress(comboBox3.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowCustomerAddress(comboBox1.Text);
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Form load
        private void New_Transaction_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = GenerateTransactionNumber();
            LoadAdminStaffNames();

            RefreshDataSetting.AutoPopDelay = 50000;
            RefreshDataSetting.InitialDelay = 300;
            RefreshDataSetting.ReshowDelay = 500;
            RefreshDataSetting.ShowAlways = true;

            RefreshDataSetting.SetToolTip(RefreshDataDb, "Press F2 to continue.\n It backs up new data when the local database changes.");
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Generate transaction number
        public string GenerateTransactionNumber()
        {
            string prefix = "T";
            string year = DateTime.Now.ToString("yy");
            string month = DateTime.Now.ToString("MM");
            int nextSeq = 1;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT `Transaction_No.` FROM TransactionMasterfile WHERE DATE_FORMAT(TransactionDate, '%Y-%m') = DATE_FORMAT(CURDATE(), '%Y-%m') ORDER BY Transaction_ID DESC LIMIT 1; ";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    var lastTrans = command.ExecuteScalar()?.ToString();
                    if (!string.IsNullOrEmpty(lastTrans))
                    {
                        string lastSeqPart = lastTrans.Split('-')[1];
                        if (int.TryParse(lastSeqPart, out int seq))
                            nextSeq = seq + 1;
                    }
                }
            }

            string formattedSeq = nextSeq.ToString("D4");
            return $"{prefix}{year}{month}-{formattedSeq}";
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Load Admin Staff
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Load Admin Staff
        private void LoadAdminStaffNames()
        {
            comboBox2.Items.Clear();
            comboBox2.Items.Add("Select In Charge personnel"); // Add default option first

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT FirstName FROM InChargePersonnelDetails ORDER BY FirstName ASC;";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        comboBox2.Items.Add(reader["FirstName"].ToString());
                }
            }

            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.SelectedIndex = 0; // Default to "Select Admin"
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // OK Button 
        private void OK_button2_Click(object sender, EventArgs e)
        {

            string transactionNo = richTextBox1.Text.Trim();
            string adminStaffName = comboBox2.SelectedItem.ToString();
            string securityGuardName = textBox1.Text.Trim();
            string customerName = "";
            string customerAddress = "";

            if (radioButton1.Checked)
            {
                customerName = comboBox1.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(customerName) || customerName == "Select Company Name")
                {
                    MessageBox.Show("Please select a company name", "Empty Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                customerAddress = richTextBox2.Text.Trim();
            }
            else if (radioButton2.Checked)
            {
                customerName = comboBox3.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(customerName) || customerName == "Select Employee Name")
                {
                    MessageBox.Show("Please select a customer Employee name", "Empty Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                customerAddress = richTextBox2.Text.Trim();
            }
            else
            {
                MessageBox.Show("Please select a customer type (Company or Employee).", "Missing Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(adminStaffName) || adminStaffName == "Select personnel")
            {
                MessageBox.Show("Please select In charge personnel name.", "Empty Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (string.IsNullOrWhiteSpace(securityGuardName))
            {
                MessageBox.Show("Please enter the Security Guard name.", "Empty Box", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Open TransactionScreen
            TransactionScreen transacScreen = new TransactionScreen(mainMenu, transactionNo, customerName, adminStaffName, securityGuardName);
            transacScreen.FormClosed += (s, args) =>
            {
                mainMenu.Show();
                mainMenu.ShowMainMenuUI(); 
            };
            transacScreen.Show();
            mainMenu.Hide();
            this.Close();

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Navigation and keypress.
        //===========================================================================
        private void BackToTheMainMenu(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                mainMenu.ShowMainMenuUI();
                this.Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                OK_button2_Click(sender, e);
            }
        }
        private void RefreshData(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F2)
            {
                RefreshDataDb_Click(sender, e);
            }
            
        }
        //===========================================================================
        private void Cancel_button1_Click(object sender, EventArgs e)
        {
            
            mainMenu.ShowMainMenuUI();
            this.Close();
        }
        //===========================================================================
        private void selectCompanyRadioButton (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F5)
            {
                radioButton1.Checked = true;
                e.SuppressKeyPress = true;
                comboBox1.Focus();
                comboBox1.DroppedDown = true;
                
            }
        }
        //===========================================================================
        private void selectEmployeeRadioButton (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F6)
            {
                radioButton2.Checked = true;
                e.SuppressKeyPress = true;
                comboBox3.Focus();
                comboBox3.DroppedDown = true;
            }
        }
        //===========================================================================
        private void selectAdminNameCombo (object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                e.SuppressKeyPress = true;
                comboBox2.Focus();
                comboBox2.DroppedDown = true;
            }
        }
        //===========================================================================
        private void SelectSecuText (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F8)
            {
                e.SuppressKeyPress = true;
                textBox1.Focus();
               

            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
       /* private void New_Transaction_FormClosed(object sender, FormClosedEventArgs e)
        //This is for x button form once clicked, it will go back to TransactionScreen.
        {
            mainMenu.Show();

        }
       */
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //The function below is to disable the textbox1 if there is no selected name lists of the company except the user select one company name
        private void Hauler_comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox1.SelectedItem?.ToString();
            if (selected == "Select Company Name" || string.IsNullOrWhiteSpace(selected))
            {
                textBox1.Enabled = false;
                textBox1.BackColor = Color.LightGray;
                richTextBox2.Clear();
            }
            else
            {
                textBox1.Enabled = true;
                textBox1.BackColor = Color.White;
                ShowCustomerAddress(selected);
            }

        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //The function below is to disable the textbox1 if there is no selected name lists of the employee except the user select one employee's name
        private void Employee_comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBox3.SelectedItem?.ToString();
            if (selected == "Select Employee Name" || string.IsNullOrWhiteSpace(selected))
            {
                textBox1.Enabled = false;
                textBox1.BackColor = Color.LightGray;
                richTextBox2.Clear();
            }
            else
            {
                textBox1.Enabled = true;
                textBox1.BackColor = Color.White;
                ShowCustomerAddress(selected);
            }

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void RefreshEssentialData()
        {
            // Refresh transaction number
            richTextBox1.Text = GenerateTransactionNumber();

            // Refresh admin staff / in-charge personnel
            LoadAdminStaffNames();

            // Refresh customer lists depending on selected type
            if (radioButton1.Checked)
            {
                LoadCustomers(2, comboBox1); // Company
            }
            else if (radioButton2.Checked)
            {
                LoadCustomers(1, comboBox3); // Employee
            }

            // Reset dependent fields
            richTextBox2.Clear();
            textBox1.Clear();
            textBox1.Enabled = false;
            textBox1.BackColor = Color.LightGray;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void RefreshDataDb_Click(object sender, EventArgs e)
        {
            try
            {
                RefreshEssentialData();
                MessageBox.Show(
                    "Data successfully refreshed from database.",
                    "Refresh Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to refresh data.\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Isolate these voids for future functions.
        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void Admin_Staff_comboBox2_SelectedIndexChanged(object sender, EventArgs e) {
       
        
        }
        private void label1_Click(object sender, EventArgs e) { }
        private void richTextBox2_TextChanged(object sender, EventArgs e) { }
        private void panel1_Paint(object sender, PaintEventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void Secu_textBox1_TextChanged(object sender, EventArgs e) { }
        private void EmployeeAddress_label6_Click(object sender, EventArgs e) { }
        private void Address_richTextBox2_TextChanged_1(object sender, EventArgs e){ }
        private void panel2_Paint(object sender, PaintEventArgs e){ }

    }
}
