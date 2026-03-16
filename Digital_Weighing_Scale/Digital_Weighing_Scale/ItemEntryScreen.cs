using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Deployment.Application;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using static System.Windows.Forms.LinkLabel;
using System.Runtime.CompilerServices;
//using System.Reflection.Emit;

namespace Digital_Weighing_Scale
{

    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public partial class ItemEntryScreen : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        // string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        //string connectionString = "server=localhost;user=root;password=master;database=Scraps";

        private ToolTip refreshToolTip = new ToolTip();
        private ToolTip StartWeightRecord = new ToolTip();
        private ToolTip CancelWeight = new ToolTip();

        private TransactionScreen transactionScreen;
        private TransactionScreen ScreenShow;
        private TransactionScreen backToTransactionScreen;
        private SerialPort sharedPort;
        private DataTable allItemsTable;

        //========================================================
        private Label blinkingLabel;
        private Timer blinkTimer;
        private bool isColorToggled = false;
        //========================================================
        private bool isReductionEnabled = false;
        private decimal reductionPercent = 0.15m; // default 15%

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ResetWeighingState()
        {

            richTextBox1.Text = "";
            textBox2.Text = "";
            comboBox1.SelectedIndex = 0;
        }
        public void UpdateLiveWeight(string displayText)
        {
            // Always update UI on the UI thread===============================================
            if (this.IsDisposed || !this.IsHandleCreated) return;
            //=================================================================================
            try
            {
                this.BeginInvoke(new Action(() =>
                {
                    if (!string.IsNullOrEmpty(displayText))
                    {
                        richTextBox1.Text = displayText;
                    }
                }));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public ItemEntryScreen(TransactionScreen ts)
        {
            InitializeComponent();
            transactionScreen = ts;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public ItemEntryScreen(TransactionScreen parentScreen, TransactionScreen backToTransaction, SerialPort port)
        {
            InitializeComponent();
            sharedPort = port;
            DisableReduction.Checked = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ItemEntryScreen_FormClosing);
            this.transactionScreen = parentScreen;
            this.StartPosition = FormStartPosition.CenterScreen;
            backToTransactionScreen = backToTransaction;
            this.KeyPreview = true;
            this.KeyDown += BackToTheTransactionScreen;
            this.KeyDown += StartWeighingRecord;
            this.KeyDown += SelectTypeItem;
            this.KeyDown += SelectComboItem;
            this.KeyDown += SelectItemCodeTxtBox;
            this.KeyDown += SelectBoxTypeCombo;
            SetupItemDetailsGrid();
            this.KeyDown += dataGridView1ScrollControl;
            this.KeyDown += RefreshDataKeys;



            //==============================BLink function===================================
            blinkingLabel = new Label
            {
                Text = "Ensure the item is placed properly on the connected digital scale.",
                AutoSize = true,
                Location = new Point(10, 920), //(x,y)
                Font = new Font("Arial", 30, FontStyle.Bold),
                ForeColor = Color.Red
            };
            this.Controls.Add(blinkingLabel);
            blinkTimer = new Timer { Interval = 500 };
            blinkTimer.Tick += BlinkTimer_Tick;
            blinkTimer.Start();
            //===============================================================================
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
        private void SharedPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = sharedPort.ReadLine().Trim();


                string cleanedData = data
                    .Replace("US", "")
                    .Replace("GS", "")
                    .Replace("ST", "")
                    .Replace(",", "")
                    .Trim();

                string numberPart = new string(cleanedData.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
                string unitPart = cleanedData.Substring(numberPart.Length).Trim();

                this.BeginInvoke(new Action(() =>
                {
                    if (decimal.TryParse(numberPart, out decimal weightValue))
                    {

                        richTextBox1.Text = $"{weightValue} {unitPart}";
                    }
                }));
            }
            catch { }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ReceiveStableWeight(string weight, TransactionScreen screenRef)
        {
            ScreenShow = screenRef;
            richTextBox1.Text = $"{weight}";
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (sharedPort != null)
                sharedPort.DataReceived -= SharedPort_DataReceived;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SetupItemDetailsGrid()
        {


            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.SteelBlue;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            //===========================grid display design==================================================
            // Header styling
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkBlue;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            // Grid appearance
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.RowTemplate.Height = 35;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            //Alternating row colors
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.AliceBlue;
            //================================================================================================
        }
        //===========================================Keycode=====================================================================================
        //--------------------------------
        private void BackToTheTransactionScreen(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                backToTransactionScreen.Show();
                this.Hide();
            }
        }
        //--------------------------------
        private void RefreshDataKeys(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                RefreshDataDb1_Click(sender, e);
            }
        }
        private void StartWeighingRecord(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, e);
            }
        }
        //--------------------------------
        private void GoBackTo_button2_Click(object sender, EventArgs e)
        {

            backToTransactionScreen.Show();
            this.Hide();
        }
        //--------------------------------
        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            blinkingLabel.ForeColor = isColorToggled ? Color.Red : Color.Blue;
            isColorToggled = !isColorToggled;
        }
        //--------------------------------
        private void SelectTypeItem(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F6)
            {
                radioButton1.Checked = true;
                e.SuppressKeyPress = true;
                textBox2.Focus();
            }

        }
        //--------------------------------
        private void SelectComboItem(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                radioButton2.Checked = true;
                e.SuppressKeyPress = true;
                comboBox2.Focus();
                comboBox2.DroppedDown = true;
            }
        }
        //--------------------------------
        private void SelectItemCodeTxtBox(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                textBox1.Focus();
                e.SuppressKeyPress = true;
            }
        }
        //--------------------------------
        private void SelectBoxTypeCombo(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F8)
            {
                comboBox1.Focus();
                comboBox1.DroppedDown = true;
                e.SuppressKeyPress = true;
            }
        }
        //=======================================================================================================================================


        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ItemEntryScreen_Load(object sender, EventArgs e)
        {
            loadBoxType();
            LoadItemDescriptionsComboBox2();
            comboBox2.SelectedIndexChanged += ComboBox2_SelectedIndexChanged;


            comboBox1.Enabled = false;
            richTextBox1.Font = new Font("Consolas", 120, FontStyle.Bold);
            richTextBox1.ForeColor = Color.DarkBlue;
            richTextBox1.Text = "0.00 kg";


            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 18, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Blue;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            LoadAllItems(); // Load all items into the grid
            SetupDataGridViewForFullRowClick();

            comboBox2.Enabled = false;
            textBox2.Enabled = false;
            WeightReduction.Enabled = false;
            //================================================================================
            refreshToolTip.AutoPopDelay = 50000;   // stays visible (50 sec)
            refreshToolTip.InitialDelay = 300;   // wait before showing 
            refreshToolTip.ReshowDelay = 500;     // time between tooltip shows
            refreshToolTip.ShowAlways = true;     // show even if the form is inactive

            StartWeightRecord.AutoPopDelay = 50000;
            StartWeightRecord.InitialDelay = 300;
            StartWeightRecord.ReshowDelay = 500;
            StartWeightRecord.ShowAlways = true;

            CancelWeight.AutoPopDelay = 5000;
            CancelWeight.InitialDelay = 300;
            CancelWeight.ReshowDelay = 500;
            CancelWeight.ShowAlways = true;
            //=================================================================================

            // Attach tooltip to the button
            refreshToolTip.SetToolTip(RefreshDataDb1, "Press F2 to continue.\n It backs up new data when the local database changes.");
            StartWeightRecord.SetToolTip(button1, "Press F1 to Start");
            CancelWeight.SetToolTip(button2, "Press Esc to Cancel");
            //================================================================================
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void LoadAllItems()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ItemCode, ItemDescription FROM ItemMasterfile ORDER BY ItemDescription ASC;";
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        allItemsTable = new DataTable();
                        adapter.Fill(allItemsTable);
                        dataGridView1.DataSource = allItemsTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database Error: " + ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ItemEntryScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                backToTransactionScreen.Show();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ItemCodeType_textBox1_TextChanged(object sender, EventArgs e)
        {

            string searchText = textBox1.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                dataGridView1.DataSource = allItemsTable;
            }
            else
            {
                var filteredRows = allItemsTable.AsEnumerable()
                    .Where(r =>
                        (r.Field<string>("ItemCode") != null && r.Field<string>("ItemCode").ToUpper().StartsWith(searchText.ToUpper()))


                    );

                dataGridView1.DataSource = filteredRows.Any() ? filteredRows.CopyToDataTable() : allItemsTable.Clone();
            }
            textBox3.Clear();
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void loadBoxType()
        {
            comboBox1.Items.Clear();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT BoxTypeDescription FROM BoxTypeMasterfile ORDER BY BoxTypeDescription ASC;";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        comboBox1.Items.Add(reader["BoxTypeDescription"].ToString());
                    }
                }
            }

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void LoadItemDescriptionsComboBox2()
        {
            comboBox2.Items.Clear();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT ItemCode, ItemDescription FROM ItemMasterfile ORDER BY ItemDescription ASC;";
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            comboBox2.Items.Add(new
                            {
                                ItemCode = reader["ItemCode"].ToString(),
                                ItemDescription = reader["ItemDescription"].ToString()
                            });
                        }
                    }
                }

                comboBox2.DisplayMember = "ItemDescription";
                comboBox2.ValueMember = "ItemCode";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading items for ComboBox2: " + ex.Message);
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem == null) return;

            dynamic selectedItem = comboBox2.SelectedItem;
            string selectedCode = selectedItem.ItemCode;
            string selectedDesc = selectedItem.ItemDescription;

            // Update TextBoxes===========================================
            textBox1.Text = selectedCode;
            textBox2.Text = selectedDesc;
            //============================================================
            // Update DataGridView selection
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["ItemCode"].Value != null &&
                    row.Cells["ItemCode"].Value.ToString() == selectedCode)
                {
                    row.Selected = true;
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
            //============================================================
            comboBox1.Enabled = true;
            UpdateSelectionDisplay();

        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ItemDetails_dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                string itemCode = row.Cells["ItemCode"].Value?.ToString() ?? "";
                string itemDescription = row.Cells["ItemDescription"].Value?.ToString() ?? "";

                textBox1.Text = itemCode;
                textBox2.Text = itemDescription;
                comboBox1.Enabled = true;

                UpdateSelectionDisplay();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void BoxType_comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectionDisplay();
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private decimal GetBoxWeightFromDatabase(string boxType)
        {
            if (boxType == "No included Box")
                return 0m;

            switch (boxType)
            {
                case "Small Box": return 0.1m;
                case "Medium Box": return 0.25m;
                case "Large Box": return 0.5m;
                default: return 0;
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SetupDataGridViewForFullRowClick()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.CellClick += ItemDetails_dataGridView1_CellContentClick;
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SearchItem_textBox2_TextChanged(object sender, EventArgs e)
        {
            string searchWord = textBox2.Text.Trim();

            if (string.IsNullOrEmpty(searchWord))
            {
                // Show all items if nothing typed
                dataGridView1.DataSource = allItemsTable;
            }
            else
            {

                var filteredRows = allItemsTable.AsEnumerable()
                    .Where(r => r.Field<string>("ItemDescription") != null &&
                                r.Field<string>("ItemDescription")
                                 .ToUpper()
                                 .StartsWith(searchWord.ToUpper()));

                dataGridView1.DataSource = filteredRows.Any() ? filteredRows.CopyToDataTable() : allItemsTable.Clone();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            comboBox1.Enabled = true;
            textBox1.Clear();
            textBox2.Clear();

            string boxType = comboBox1.SelectedItem != null ? comboBox1.SelectedItem.ToString() : "";
            decimal boxWeight = GetBoxWeightFromDatabase(boxType);
            richTextBox2.Text = $"Box Type: {boxType}\nBox Weight: {boxWeight} kg";
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e)
        {

            string itemDesc = !string.IsNullOrWhiteSpace(textBox3.Text) ? textBox3.Text.Trim() : textBox2.Text.Trim();

            if (string.IsNullOrEmpty(itemDesc))
            {
                MessageBox.Show("Please enter or select an item description.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Please select a box type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string boxType = comboBox1.SelectedItem.ToString();
            string rawWeightText = richTextBox1.Text.Trim();
            decimal finalWeight = 0m;

            // Extract numeric weight only
            string numericPart = new string(rawWeightText
                .TakeWhile(c => char.IsDigit(c) || c == '.')
                .ToArray());

            if (!decimal.TryParse(numericPart, out decimal rawWeight))
            {
                MessageBox.Show("Invalid weight value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            // Apply reduction if enabled===================================
            if (isReductionEnabled && reductionPercent > 0)
            {
                // FINAL OUTPUT = raw weight × reduction %
                finalWeight = rawWeight * reductionPercent;
            }
            else
            {
                finalWeight = rawWeight;
            }
            //==============================================================

            // Preserve unit (kg)===============================================================================
            string finalWeightText = $"{finalWeight:F2} kg";


            if (string.IsNullOrWhiteSpace(textBox3.Text))
            {
                bool isValidItem = false;

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "SELECT COUNT(*) FROM ItemMasterfile WHERE ItemDescription = @ItemDesc";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ItemDesc", itemDesc);
                            int count = Convert.ToInt32(cmd.ExecuteScalar());
                            if (count > 0)
                                isValidItem = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message);
                    return;
                }

                if (!isValidItem)
                {
                    MessageBox.Show("The item you typed does not match any record in the database. Please select a valid item.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }


            transactionScreen.AddItemToGrid(itemDesc, boxType, finalWeightText);

            decimal boxWeight = 0m;
            if (boxType != "No included Box")
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "SELECT BoxWeight FROM BoxTypeMasterfile WHERE BoxTypeDescription = @BoxTypeDesc LIMIT 1";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@BoxTypeDesc", boxType);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                                boxWeight = Convert.ToDecimal(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error fetching box weight: " + ex.Message);
                }
            }

            transactionScreen.UpdateBoxWeight(boxWeight);

            this.Close();
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void UpdateSelectionDisplay()
        {
            string itemCode = textBox1.Text.Trim();
            string itemDesc = textBox2.Text.Trim();
            string boxType = comboBox1.SelectedItem != null ? comboBox1.SelectedItem.ToString() : "";
            decimal boxWeight = 0m;

            if (!string.IsNullOrEmpty(boxType))
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string query = "SELECT BoxWeight FROM BoxTypeMasterfile WHERE BoxTypeDescription = @BoxTypeDesc LIMIT 1";
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@BoxTypeDesc", boxType);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                                boxWeight = Convert.ToDecimal(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error fetching box weight: " + ex.Message);
                }
            }

            // richTextBox2.Text = $"Item Code: {itemCode}\nItem Description: {itemDesc}\nBox Type: {boxType}\nBox Weight: {boxWeight} kg";
            string reductionText = "";

            if (isReductionEnabled)
            {
                int percentDisplay = (int)(reductionPercent * 100);
                reductionText = $"\nReduction by: {percentDisplay}%";
            }

            richTextBox2.Text =
                $"Item Code: {itemCode}" +
                $"\nItem Description: {itemDesc}" +
                $"\nBox Type: {boxType}" +
                $"\nBox Weight: {boxWeight} kg" +
                reductionText;

        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void EnableReduction_CheckedChanged(object sender, EventArgs e)
        {
            if (EnableReduction.Checked)
            {
                isReductionEnabled = true;

                WeightReduction.Enabled = true;
                WeightReduction.Minimum = 0;
                WeightReduction.Maximum = 100;
                WeightReduction.DecimalPlaces = 0;
                WeightReduction.Value = 15; // default 15%

                reductionPercent = WeightReduction.Value / 100m;


                comboBox1.Enabled = true;
                UpdateSelectionDisplay();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void DisableReduction_CheckedChanged(object sender, EventArgs e)
        {
            if (DisableReduction.Checked)
            {
                isReductionEnabled = false;
                WeightReduction.Enabled = false;
                reductionPercent = 0m;
                UpdateSelectionDisplay();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void WeightReduction_ValueChanged(object sender, EventArgs e)
        {
            reductionPercent = WeightReduction.Value / 100m;
            UpdateSelectionDisplay();
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Select_radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {

                comboBox2.Enabled = true;
                textBox2.Enabled = false;
                textBox2.Clear();
                comboBox2.Focus();
                textBox3.Clear();
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void RefreshDataDb1_Click(object sender, EventArgs e)
        {
            try
            {
                // Refresh Box Types
                loadBoxType();

                // Refresh Item Descriptions in ComboBox2
                LoadItemDescriptionsComboBox2();

                // Refresh DataGridView with all items
                LoadAllItems();

                MessageBox.Show("Data refreshed successfully!", "Refresh Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error refreshing data: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Type_radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                textBox2.Enabled = true;
                comboBox2.Enabled = false;
                textBox2.Focus();
                textBox3.Clear();
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Isolate these voids for future functions.
        private void WeightDisplay_richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void timer1_Tick(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void ItemDescription_comboBox2_SelectedIndexChanged(object sender, EventArgs e) { }
        private void ConfirmDetails_richTextBox2_TextChanged(object sender, EventArgs e) { }

    }
}

