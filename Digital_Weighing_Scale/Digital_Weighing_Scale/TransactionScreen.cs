using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO.Ports;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Timers;
using System.Windows.Forms;


namespace Digital_Weighing_Scale
{
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public partial class TransactionScreen : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        //string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        //string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        //string connectionString = "server=localhost;user=root;password=masterx;database=Scraps";


        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
        private ItemEntryScreen outputWeight;
        //====================private constructors for digital scale======================
        private System.Windows.Forms.Timer reconnectTimer;
        private System.Windows.Forms.Timer stableCheckTimer;
        private SerialPort serialPort;
        private string selectedPort = null;
        private readonly object portLock = new object();
        private bool isTryingToReconnect = false;
        private string lastWeight = "";
        private DateTime lastChangeTime;
        private bool weightSaved = false;
        private bool isWeighingActive = false;
        private decimal previousWeight = 0m;
        private bool isWeighingPaused = false;
        private System.Windows.Forms.Timer blinkTimer;
        private bool isBlinkOn = true;
        private Form overlayForm;


        //================================================================================

        private Main_Menu mainMenuForm;
        private ItemEntryScreen itemEntryForm;
        private string transactionNo, customerName, adminStaffName, securityGuardName, transactionDate;
        private int retryRowIndex = -1;

        private bool isHovering1 = false;
        private bool isHovering2 = false;
        private int currentBaudRate = 9600; 
        public int CurrentTransactionID { get; set; }

        public DataGridView TransactionGrid => dataGridView1;
        public bool allowClose = false;
        public string TotalWeightsText
        {
            get { return richTextBox6.Text; }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public TransactionScreen(Main_Menu mainMenu, string transactionNo, string customerName, string adminStaffName, string securityGuardName)
        {
            InitializeComponent();

           tabControl1.ItemSize = new Size(200, 50); 
            tabControl1.SizeMode = TabSizeMode.Fixed; 
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.DrawItem += TabControl1_DrawItem;

            this.mainMenuForm = mainMenu;
            this.transactionNo = transactionNo;
            this.customerName = customerName;
            this.adminStaffName = adminStaffName;
            this.securityGuardName = securityGuardName;
            this.transactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            this.KeyDown += CancelTransactionForm;
            this.FormClosing += TransactionScreen_FormClosing;
            this.KeyPreview = true;
            this.KeyDown += SaveTransaction;
            this.KeyDown += RemoveItem;
            this.KeyDown += dataGridView1ScrollControl;
            this.KeyDown += Press_A;
            this.KeyDown += Press_B;
            dataGridView1.ReadOnly = true;
      

            pictureBox2.Cursor = Cursors.Hand;
            pictureBox3.Cursor = Cursors.Hand;
            // this.pictureBox1.Visible = false;
            // this.pictureBox4.Visible = true;
            this.panel1.Visible = false;
            this.panel3.Visible = true;


            pictureBox2.MouseEnter += PictureBox2_MouseEnter;
            pictureBox2.MouseLeave += PictureBox2_MouseLeave;
            pictureBox3.MouseLeave += PictureBox3_MouseLeave;
            pictureBox3.MouseEnter += PictureBox3_MouseEnter;
            pictureBox2.Paint += PictureBox2_Paint;
            pictureBox3.Paint += PictureBox3_Paint;

      
            SetupRichTextBoxes();
            SetupDataGridView();

            // Set initial info==================================================
            richTextBox1.Text = $"Date: {transactionDate}";
            richTextBox2.Text = $"Customer: {customerName}";
            richTextBox3.Text = $"Transaction No: {transactionNo}";
            richTextBox4.Text = $"In-charge: {adminStaffName}";
            richTextBox5.Text = $"Guard: {securityGuardName}";
            //===================================================================

            //This is for instruction embedded to richtextbox7===================
            richTextBox7.Text = $"Reminder: Always tared the scale to zero. \nPaalala: Laging I-tare ang timbangan sa zero.";
            richTextBox8.Text = $"FI05 Fuji Scale Device is either turned off, disconnected, or using a different digital scale. If you are using a different scale, such as the Fuji DM2, change the digital serial connection on the upper-left tab next to the HOME tab.";
            //===================================================================
            richTextBox9.Text = "Connected Device:\nFI05 Fuji (DEFAULT System) \nSerial Baud rate: 9600 \nCapacity: 60-600 Kg";
            richTextBox10.Text = "Connected Device:\nDM2-20KX Fuji \nSerial Baud rate: 2400 \nCapacity: 20 Kg";
            richTextBox6.Text = "Total Items:\nTotal Box Weight:\nTotal Gross Weight:\nTotal Net Weight:";
            //===================================================================
            // Initialize the blink timer for reminder when using the scale and system
            blinkTimer = new System.Windows.Forms.Timer();
            blinkTimer.Interval = 500; 
            blinkTimer.Tick += BlinkTimer_Tick;
            blinkTimer.Start();


        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string GetItemList()
        {
            StringBuilder sb = new StringBuilder();
            int counter = 1;

            bool hasIsVoidColumn = TransactionGrid.Columns.Contains("IsVoid");

            foreach (DataGridViewRow row in TransactionGrid.Rows)
            {
                if (row.IsNewRow) continue;

                bool isVoid = false;

                // Check IsVoid column if exists======================================================
                if (hasIsVoidColumn)
                {
                    var val = row.Cells["IsVoid"]?.Value;
                    if (val != null && int.TryParse(val.ToString(), out int voidFlag))
                    {
                        if (voidFlag == 1) isVoid = true;
                    }
                }

                // Strikethrough font================================
                bool hasStrikethrough = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    Font cellFont = cell.Style?.Font ?? cell.InheritedStyle?.Font;
                    if (cellFont != null && cellFont.Strikeout)
                    {
                        hasStrikethrough = true;
                        break;
                    }
                }

                // Dont include voided items==================================================================
                if (isVoid || hasStrikethrough)
                    continue;

                string itemDesc = row.Cells["Item Description"].Value?.ToString() ?? "";
                string weight = row.Cells["NET Weight (KGS) - Box"].Value?.ToString() ?? "";

                sb.AppendLine($"{counter}. {itemDesc} - {weight}");
                counter++;
            }

            return sb.ToString();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void ApplyVoidEffect(int itemNo)
        {
            bool found = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;

                object cellVal = row.Cells["No."].Value;
                if (cellVal == null) continue;

                if (int.TryParse(cellVal.ToString(), out int no) && no == itemNo)
                {
                    found = true;

                    // Apply strike-through in this part==========================================================
                    foreach (DataGridViewCell cell in row.Cells)
                    {

                        Font baseFont = cell.Style.Font ?? dataGridView1.DefaultCellStyle.Font ?? this.Font;
                        Font newFont;
                        try
                        {
                            newFont = new Font(baseFont, baseFont.Style | FontStyle.Strikeout);
                        }
                        catch
                        {
                            newFont = new Font(baseFont.FontFamily, baseFont.Size, FontStyle.Strikeout);
                        }

                        //===============================================================================================


                        cell.Style.Font = newFont;                    // strike-through
                        cell.Style.ForeColor = Color.DarkGray;        // dim text
                        cell.Style.BackColor = Color.LightGray;
                    }

                    // Mark row as voided (in-memory flag)
                    row.Tag = "voided";


                    break;
                }
            }

            if (!found)
            {
                MessageBox.Show($"No row with No. = {itemNo} was found.", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            }
            UpdateTotalItems();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void UpdateTotalItems()
        {
            int totalItems = 0;
            decimal totalGrossWeight = 0m;
            decimal totalBoxWeight = 0m;
            decimal totalNetWeight = 0m;

            foreach (DataGridViewRow row in dataGridView1.Rows)
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

                if (!isVoided)
                {
                    totalItems++;

                    decimal weight = 0m;
                    decimal boxWeight = 0m;

                    object weightVal = row.Cells["NET Weight (KGS) - Box"].Value;
                    if (weightVal != null)
                    {
                        string s = new string(weightVal.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
                        decimal.TryParse(s, out weight);
                    }

                    object boxVal = row.Cells["Box Weight (KGS)"].Value;
                    if (boxVal != null)
                    {
                        string s = new string(boxVal.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
                        decimal.TryParse(s, out boxWeight);
                    }

                    totalNetWeight += weight;            
                    totalBoxWeight += boxWeight;         
                    totalGrossWeight += (weight + boxWeight); 
                }
            }

            richTextBox6.Text = $"Total Items: {totalItems}\n" +
                                $"Total Box Weight: {totalBoxWeight:0.00} KGS\n" +
                                $"Total Gross Weight: {totalGrossWeight:0.00} KGS\n" +
                                $"Total Net Weight: {totalNetWeight:0.00} KGS";

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //For Keyboard control
        private void Press_A (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.A)
            {
                pictureBox2_Click(sender, e);
            }
        }
        //===================================================================
        private void Press_B (object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.B)
            {
                pictureBox3_Click(sender, e);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void AddItemToGrid(string itemDesc, string boxType, string weight)
        {
            int rowIndex = dataGridView1.Rows.Add();
            dataGridView1.Rows[rowIndex].Cells["No."].Value = rowIndex + 1;
            dataGridView1.Rows[rowIndex].Cells["Item Description"].Value = itemDesc;
            dataGridView1.Rows[rowIndex].Cells["Box Type"].Value = boxType;
            dataGridView1.Rows[rowIndex].Cells["NET Weight (KGS) - Box"].Value = weight;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void UpdateBoxWeight(decimal boxWeight)
        {
            if (dataGridView1.Rows.Count > 0)
            {
                DataGridViewRow lastRow = dataGridView1.Rows[dataGridView1.Rows.Count - 1];


                lastRow.Cells["Box Weight (KGS)"].Value = boxWeight.ToString("0.00");

                // Get Gross Weight from Weight (KGS)
                object weightVal = lastRow.Cells["NET Weight (KGS) - Box"].Value;

                if (weightVal != null)
                {
                    string weightText = new string(weightVal.ToString()
                        .Where(c => char.IsDigit(c) || c == '.')
                        .ToArray());

                    if (decimal.TryParse(weightText, out decimal grossWeight))
                    {
                        decimal finalNet = grossWeight - boxWeight;
                        if (finalNet < 0) finalNet = 0;

                        // Update Weight (KGS) with final net result
                        lastRow.Cells["NET Weight (KGS) - Box"].Value = finalNet.ToString("0.00");
                    }
                }

                UpdateTotalItems();
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
        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            if (isBlinkOn)
            {
             
                richTextBox7.ForeColor = Color.Red;
            }
            else
            {
           
                richTextBox7.ForeColor = Color.DarkBlue;
            }

            isBlinkOn = !isBlinkOn;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void TabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl1.TabPages[e.Index];
            Rectangle tabBounds = tabControl1.GetTabRect(e.Index);

        
            Color textColor = (e.Index == tabControl1.SelectedIndex) ? Color.Red : Color.Blue;

            using (Brush backgroundBrush = new SolidBrush(page.BackColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, tabBounds);
            }

           
            using (Brush textBrush = new SolidBrush(textColor))
            {
                StringFormat stringFlags = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(page.Text, new Font("Arial", 16, FontStyle.Bold), textBrush, tabBounds, stringFlags);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ShowOverlay()
        {
            overlayForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                BackColor = Color.Gray,
                Opacity = 0.3, 
                Size = this.Size,
                Location = this.Location,
                Owner = this
            };

            overlayForm.Show();
            overlayForm.BringToFront();
        }
        //=============================================================
        private void HideOverlay()
        {
            if (overlayForm != null)
            {
                overlayForm.Close();
                overlayForm.Dispose();
                overlayForm = null;
            }
        }
        //====================================================================================Digital Weighing initial scale start function===============================================================================================
        private void ChangeBaudRate(int newBaudRate)
        {
            try
            {
               
                isWeighingPaused = true;
                stableCheckTimer?.Stop();

                lock (portLock)
                {
                 
                    if (serialPort != null && serialPort.IsOpen)
                    {
                        serialPort.DataReceived -= SerialPort_DataReceived;
                        serialPort.Close();
                    }

                    currentBaudRate = newBaudRate;

                    if (!string.IsNullOrEmpty(selectedPort))
                    {
                    
                        serialPort = new SerialPort(selectedPort, currentBaudRate, Parity.None, 8, StopBits.One)
                        {
                            Handshake = Handshake.None,
                            ReadTimeout = 500,
                            WriteTimeout = 500,
                            NewLine = "\r\n"
                        };

                        serialPort.DataReceived += SerialPort_DataReceived;
                        serialPort.Open();
                    }
                }

                MessageBox.Show($"Baud rate changed to {newBaudRate} successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl1.SelectedTab = tabPage1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to change baud rate: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
             
                isWeighingPaused = false;
                if (isWeighingActive)
                    stableCheckTimer?.Start();
            }
        }
        //=============================================================================
        public void ResumeWeighing()
        {
            TryConnectToScale();
            isWeighingActive = true;
            stableCheckTimer.Start();
            weightSaved = false;

        }
        //=============================================================================
        private void InitializeReconnectTimer()
        {
            reconnectTimer = new System.Windows.Forms.Timer();
            reconnectTimer.Interval = 3000;
            reconnectTimer.Tick += ReconnectTimer_Tick;
            reconnectTimer.Start();
        }
        //=============================================================================
        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!isWeighingActive) return; 

            if (serialPort == null || !serialPort.IsOpen)
            {
                TryReconnect();
            }
        }
        //=============================================================================
        private void TryConnectToScale()
        {
            string[] availablePorts = SerialPort.GetPortNames();
            bool connected = false;

            foreach (string port in availablePorts)
            {
                if (TryOpenPort(port))
                {
                    connected = true;
                    break;
                }
            }

            if (!connected)
            {
               
                ShowScaleConnectionError();
            }
        }

        //=============================================================================
        private bool TryOpenPort(string portName)
        {
            try
            {
                lock (portLock)
                {
                    if (serialPort != null)
                    {
                        serialPort.DataReceived -= SerialPort_DataReceived;
                        if (serialPort.IsOpen)
                            serialPort.Close();
                        serialPort.Dispose();
                    }

                    serialPort = new SerialPort(portName, currentBaudRate, Parity.None, 8, StopBits.One)
                    {
                        Handshake = Handshake.None,
                        ReadTimeout = 500,
                        WriteTimeout = 500,
                        NewLine = "\r\n"
                    };

                    serialPort.DataReceived += SerialPort_DataReceived;
                    serialPort.Open();
                    selectedPort = portName;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        //=============================================================================
        private void TryReconnect()
        {
            if (isTryingToReconnect) return;
            isTryingToReconnect = true;

            try
            {
                string[] ports = SerialPort.GetPortNames();

                // Reconnect to the same port====================================
                if (selectedPort != null && ports.Contains(selectedPort))
                {
                    if (TryOpenPort(selectedPort))
                    {
                        isTryingToReconnect = false;
                        return;
                    }
                }
                //===============================================================

                //try any available port
                foreach (string port in ports)
                {
                    if (TryOpenPort(port))
                        break;
                }
            }
            finally
            {
                isTryingToReconnect = false;
            }
        }
        //=============================================================================
        private void InitializeStableCheckTimer()
        {
            stableCheckTimer = new System.Windows.Forms.Timer();
            stableCheckTimer.Interval = 500;
            stableCheckTimer.Tick += StableCheckTimer_Tick;

        }
        //=============================================================================
        private void StableCheckTimer_Tick(object sender, EventArgs e)
        {
            if (!isWeighingActive || string.IsNullOrEmpty(lastWeight) || weightSaved) return;

            // Only save if weight is above threshold like (weightValue greater than 0.05m)
            string[] parts = lastWeight.Split(' ');
            if (decimal.TryParse(parts[0], out decimal weightValue) && weightValue > 0.05m)
            {
                weightSaved = true;
                SaveStableWeight(lastWeight);  // pass full string with unit like kg or g
            }
        }

        //=============================================================================
        private void SaveStableWeight(string stableWeight)
        {
            
            isWeighingActive = false;
            stableCheckTimer.Stop();

            try
            {
                //  Only create the second form once==============================
                if (outputWeight == null || outputWeight.IsDisposed)
                {
                    outputWeight = new ItemEntryScreen(this, this, serialPort);
                    outputWeight.Show();
                }
                //================================================================
                else
                {
                    // If already open, just bring it to front==============
                    if (!outputWeight.Visible)
                        outputWeight.Show();
                    outputWeight.BringToFront();
                    //======================================================
                }

                
                outputWeight.ReceiveStableWeight(stableWeight, this);

                ResetWeighingState();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving stable weight: {ex.Message}");
            }
        }

        //=============================================================================
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadLine().Trim();
                string cleanedData = data


                            .Replace("US", "")
                            .Replace("GS", "")
                            .Replace("ST", "")
                            .Replace(",", "")
                            .Trim();

                string numberPart = new string(cleanedData.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
                string unitPart = cleanedData.Substring(numberPart.Length).Trim();

                if (decimal.TryParse(numberPart, out decimal weightValue))
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lastWeight = $"{weightValue} {unitPart}";
                        UpdateZeroIndicator(weightValue);

                        if (!isWeighingPaused)  
                        {
                            try
                            {
                                if (outputWeight != null && !outputWeight.IsDisposed && outputWeight.Visible)
                                {
                                    outputWeight.UpdateLiveWeight(lastWeight);
                                }
                                if (itemEntryForm != null && !itemEntryForm.IsDisposed && itemEntryForm.Visible)
                                {
                                    itemEntryForm.UpdateLiveWeight(lastWeight);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Forwarding weight failed: {ex.Message}");
                            }

                            if (weightValue > 0.05m && previousWeight < 0.05m)
                            {
                                isWeighingActive = true;
                                stableCheckTimer.Start();
                                weightSaved = false;
                                lastChangeTime = DateTime.Now;
                            }

                            if (previousWeight > 0.1m && weightValue < previousWeight && weightValue < 0.05m)
                            {
                                ResetWeighingState();
                            }

                            if (Math.Abs(weightValue - previousWeight) > 0.01m)
                            {
                                lastChangeTime = DateTime.Now;
                                weightSaved = false;
                            }

                            previousWeight = weightValue;
                        }
                    }));
                }

            }
            catch
            {
                BeginInvoke(new Action(() => SafeClosePort()));
            }
        }

        //=============================================================================
        private void SafeClosePort()
        {
            lock (portLock)
            {
                try
                {
                    if (serialPort != null)
                    {
                        serialPort.DataReceived -= SerialPort_DataReceived;

                        if (serialPort.IsOpen)
                            serialPort.Close();

                        serialPort.Dispose();
                        serialPort = null;
                    }
                }
                catch { }
            }
        }

        //=============================================================================
        public void ResetWeighingState()
        {
            isWeighingActive = false;
            weightSaved = false;
            lastWeight = "";


        }
        //====================================================================================Digital Weighing initial scale end function=================================================================================================
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void AddWeightToGrid(string weight)
        {

            int rowNumber = dataGridView1.Rows.Count + 1;
            dataGridView1.Rows.Add(rowNumber, "", weight, "", "");
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SetupDataGridView()
        {
            dataGridView1.ColumnCount = 5;
            dataGridView1.Columns[0].Name = "No.";
            dataGridView1.Columns[1].Name = "Item Description";
            dataGridView1.Columns[2].Name = "NET Weight (KGS) - Box";
            dataGridView1.Columns[3].Name = "Box Type";
            dataGridView1.Columns[4].Name = "Box Weight (KGS)";
            dataGridView1.AllowUserToAddRows = false;


            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.RowTemplate.Height = 40;

            // ===== Font and Style ====================================================================
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 18, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Blue;
            dataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // ===== Header Style ======================================================================
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 18, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkBlue;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.EnableHeadersVisualStyles = false;

            // Specific Column Width Ratios ============================================================
            dataGridView1.Columns[0].FillWeight = 10;  // No.
            dataGridView1.Columns[1].FillWeight = 40;  // Item Description
            dataGridView1.Columns[2].FillWeight = 20;  // Weight (KGS)
            dataGridView1.Columns[3].FillWeight = 15;  // Box Type
            dataGridView1.Columns[4].FillWeight = 15;  // Box Weight (KGS)
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void UpdateZeroIndicator(decimal weightValue)
        {
            if (weightValue == 0.00m)
            {
                richTextBox8.BackColor = Color.Green;
                richTextBox8.ForeColor = Color.White;
                richTextBox8.Font = new Font("Arial", 20, FontStyle.Bold); 
                richTextBox8.Text = "Place the item(s) on the digital scale.\nThe system will automatically display the output.";
            }
            else
            {
                richTextBox8.BackColor = Color.Red;
                richTextBox8.ForeColor = Color.Black;
                richTextBox8.Font = new Font("Arial", 20, FontStyle.Bold);
                richTextBox8.Text = "The digital scale has a load. Remove the item(s) and tare to zero before starting another entry. ";
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public void ReceiveStableWeight(string weight, string itemDescription, string boxType)
        {
            if (retryRowIndex >= 0)
            {
             
                dataGridView1.Rows[retryRowIndex].Cells[1].Value = itemDescription;
                dataGridView1.Rows[retryRowIndex].Cells[2].Value = weight;
                dataGridView1.Rows[retryRowIndex].Cells[3].Value = boxType;
                retryRowIndex = -1; // reset after retry=================================
            }
            else
            {
             
                int rowNumber = dataGridView1.Rows.Count + 1;
                dataGridView1.Rows.Add(rowNumber, itemDescription, weight, boxType, "");
            }
            UpdateTotalItems();
            this.Show();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SetupRichTextBoxes()
        {
            richTextBox1.ReadOnly = true; richTextBox1.Font = new Font("Arial", 22, FontStyle.Bold); richTextBox1.ForeColor = Color.DarkRed;
            richTextBox2.ReadOnly = true; richTextBox2.Font = new Font("Arial", 25, FontStyle.Bold); richTextBox2.ForeColor = Color.DarkRed;
            richTextBox3.ReadOnly = true; richTextBox3.Font = new Font("Arial", 25, FontStyle.Bold); richTextBox3.ForeColor = Color.DarkRed;
            richTextBox4.ReadOnly = true; richTextBox4.Font = new Font("Arial", 25, FontStyle.Bold); richTextBox4.ForeColor = Color.DarkRed;
            richTextBox5.ReadOnly = true; richTextBox5.Font = new Font("Arial", 25, FontStyle.Bold); richTextBox5.ForeColor = Color.DarkRed;
            richTextBox6.ReadOnly = true; richTextBox6.Font = new Font("Arial", 20, FontStyle.Bold); richTextBox6.ForeColor = Color.DarkBlue;
            richTextBox7.ReadOnly = true; richTextBox7.Font = new Font("Arial", 20, FontStyle.Bold); richTextBox7.ForeColor = Color.DarkBlue;



        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //===========================================================================
        // When the mouse enters pictureBox2
        private void PictureBox2_MouseEnter(object sender, EventArgs e)
        {
            isHovering1 = true;
            pictureBox2.Invalidate();
        }
        //================================
        private void PictureBox2_MouseLeave(object sender, EventArgs e)
        {
            isHovering1 = false;
            pictureBox2.Invalidate();
        }
        //================================
        private void PictureBox3_MouseEnter(object sender, EventArgs e)
        {
            isHovering2 = true;
            pictureBox3.Invalidate();
        }
        //================================
        private void PictureBox3_MouseLeave(object sender, EventArgs e)
        {
            isHovering2 = false;
            pictureBox3.Invalidate();
        }
        //================================
        private void PictureBox2_Paint(object sender, PaintEventArgs e)
        {
            if (isHovering1)
            {
                int borderWidth = 3;
                Color borderColor = Color.Blue;
                using (Pen pen = new Pen(borderColor, borderWidth))
                {

                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox2.Width - 1, pictureBox2.Height - 1);
                }
            }
        }
        //=============================================================================================================
        private void PictureBox3_Paint(object sender, PaintEventArgs e)
        {
            if (isHovering2)
            {
                int borderWidth = 3;
                Color borderColor = Color.Blue;
                using (Pen pen = new Pen(borderColor, borderWidth))
                {

                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox3.Width - 1, pictureBox3.Height - 1);
                }
            }
        }
        //===========================================================================
        //=============================functions for KEyboard control below==================================================

        //-------------------------------------------------------------
       
        private void CancelTransactionForm(object sender, KeyEventArgs e)
        {
            ShowOverlay();
            isWeighingPaused = true;
            if (e.KeyCode == Keys.Escape)
            {
                CancelTrans_button3_Click_1(sender, e);
            }
            isWeighingPaused = false;
             HideOverlay();
        }

        //-------------------------------------------------------------
        private void SaveTransaction(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, e);
            }
        }
        //-------------------------------------------------------------
        private void RemoveItem(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F2)
            {
                button4_Click(sender, e);
            }
        }
        //-------------------------------------------------------------
        private void button4_Click(object sender, EventArgs e)
        {
            ShowOverlay();
            // Pause
            isWeighingPaused = true;

            using (isVoid excludeItem = new isVoid(this))
            {
                excludeItem.ShowDialog();
            }

            // Resume
            isWeighingPaused = false;
            HideOverlay();
        }
        //-------------------------------------------------------------
        //=============================functions for KEyboard control on top==================================================
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void TransactionScreen_FormClosing(object sender, FormClosingEventArgs e)
        {
           
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {

                e.Cancel = true;
                ShowOverlay();
                isWeighingPaused = true;
                using (Cancel_Transaction cancelForm = new Cancel_Transaction(this, transactionNo, mainMenuForm))
                {
                    cancelForm.ShowDialog();
                }
                isWeighingPaused = false;
                HideOverlay();
            }
            else
            {
                reconnectTimer?.Stop();
                stableCheckTimer?.Stop();
                SafeClosePort();

            }

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void CancelTrans_button3_Click_1(object sender, EventArgs e)
        {
            Cancel_Transaction cancel = new Cancel_Transaction(this, transactionNo, mainMenuForm);
            cancel.ShowDialog();

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button3_Click(object sender, EventArgs e)
        {
            ShowOverlay();
            //Pause
            isWeighingPaused = true;

            using (Cancel_Transaction cancel = new Cancel_Transaction(this, transactionNo, mainMenuForm))
            {
                cancel.ShowDialog();
            }
            //Resume
            isWeighingPaused = false;
            HideOverlay();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void ShowScaleConnectionError()
        {
            richTextBox8.BackColor = Color.Yellow;
            richTextBox8.ForeColor = Color.Black;
            richTextBox8.Font = new Font("Arial", 20, FontStyle.Bold);
            richTextBox8.Text = "Serial port connected to the digital scale is unavailable.\nPlease connect the scale connection.";
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            //=====================Edit cornel radius in panel=================================================================
            int radius = 20;
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(0, 0, radius, radius, 180, 90); // Top-left corner
            path.AddArc(panel1.Width - radius, 0, radius, radius, 270, 90); // Top-right
            path.AddArc(panel1.Width - radius, panel1.Height - radius, radius, radius, 0, 90); // Bottom-right
            path.AddArc(0, panel1.Height - radius, radius, radius, 90, 90); // Bottom-left
            path.CloseFigure();
            panel3.Region = new Region(path);
            panel1.Region = new Region(path);
            
            //=================================================================================================================


            InitializeReconnectTimer();
            InitializeStableCheckTimer();
            currentBaudRate = 9600; // default

            TryConnectToScale();
            DialogResult result = MessageBox.Show("Weighing is now available.\nBefore proceeding, make sure the scale is turned on, tared to zero, and connected to the PC.\n\nClick OK to proceed.", "Proceed Weighing", MessageBoxButtons.OK, MessageBoxIcon.Information);
          
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.Resizable = DataGridViewTriState.False;
            }

            if (result == DialogResult.OK)
            {
                // Start monitoring the scale after clicking OK the info
            
                isWeighingActive = true;
                stableCheckTimer.Start();
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Font = new Font("Arial", 18, FontStyle.Regular);
            dataGridView1.DefaultCellStyle.ForeColor = Color.Blue;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            
        }
        /**/
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public void SaveTransactionToDatabase()
        {
            if (dataGridView1.Rows.Count == 0 ||
                  (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow))
            {
                MessageBox.Show(
                    "No data found in the table. Please add data before saving, or cancel the transaction.",
                    "No Data Transaction Yet",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    //  Get CustomerID========================================================================================================
                    int customerID = 0;
                    string getCustomerQuery = "SELECT Customer_ID FROM CustomerMasterfile WHERE CustomerName = @CustomerName LIMIT 1;";
                    using (MySqlCommand cmd = new MySqlCommand(getCustomerQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CustomerName", customerName);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                            customerID = Convert.ToInt32(result);
                        else
                        {
                            MessageBox.Show("Customer not found in database.");
                            return;
                        }
                    }
                    //========================================================================================================================
                    //  Get AdminStaffID======================================================================================================
                    int adminStaffID = 0;
                    string getAdminQuery = "SELECT EmployeeDetailID FROM InChargePersonnelDetails WHERE FirstName = @FirstName LIMIT 1;";
                    using (MySqlCommand cmd = new MySqlCommand(getAdminQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", adminStaffName);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                            adminStaffID = Convert.ToInt32(result);
                        else
                        {
                            MessageBox.Show("In Charge Personnel not found in database.");
                            return;
                        }
                    }
                    //========================================================================================================================
                    //  Insert TransactionMasterfile==================================================================
                    string insertTransactionQuery = @"
           INSERT INTO TransactionMasterfile 
           (`Transaction_No.`, TransactionDate, CustomerID, AdminStaffID, SecurityGuardName, DateTimeStarted)
           VALUES
           (@TransactionNo, @TransactionDate, @CustomerID, @AdminStaffID, @SecurityGuardName, @DateTimeStarted);
       ";
                    using (MySqlCommand cmd = new MySqlCommand(insertTransactionQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionNo", transactionNo);
                        cmd.Parameters.AddWithValue("@TransactionDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@CustomerID", customerID);
                        cmd.Parameters.AddWithValue("@AdminStaffID", adminStaffID);
                        cmd.Parameters.AddWithValue("@SecurityGuardName", securityGuardName);



                        // Extract only the date and time from richTextBox1. This is the previous date and time ==============================
                        string richText = richTextBox1.Text.Trim();
                        string cleanedDateText = richText.Replace("Date:", "").Trim();

                        DateTime parsedDateTime;
                        if (!DateTime.TryParse(cleanedDateText, out parsedDateTime))
                        {
                            MessageBox.Show("Invalid date format in the textbox. Please check the value.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        cmd.Parameters.AddWithValue("@DateTimeStarted", parsedDateTime);
                        cmd.ExecuteNonQuery();
                    }
                    //========================================================================================================================
                    //================================================================================================

                    //  Get last inserted TransactionID=======================================================================================
                    long transactionID = 0;
                    string getTransactionIDQuery = "SELECT Transaction_ID FROM TransactionMasterfile WHERE `Transaction_No.` = @TransactionNo LIMIT 1;";
                    using (MySqlCommand cmd = new MySqlCommand(getTransactionIDQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@TransactionNo", transactionNo);
                        transactionID = Convert.ToInt64(cmd.ExecuteScalar());
                    }

                    //========================================================================================================================
                    int itemNo = 1; // start ItemNo from 1
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string itemDescription = row.Cells["Item Description"].Value?.ToString() ?? "";
                        string boxTypeDesc = row.Cells["Box Type"].Value?.ToString() ?? "";
                        string weightStr = row.Cells["NET Weight (KGS) - Box"].Value?.ToString() ?? "";
                        string boxWeightStr = row.Cells["Box Weight (KGS)"].Value?.ToString() ?? "";

                        // Clean and parse numeric values=====================================================================================
                        decimal weight = 0;
                        decimal boxWeight = 0;

                        if (!string.IsNullOrEmpty(weightStr))
                            decimal.TryParse(new string(weightStr.Where(c => char.IsDigit(c) || c == '.').ToArray()), out weight);

                        if (!string.IsNullOrEmpty(boxWeightStr))
                            decimal.TryParse(new string(boxWeightStr.Where(c => char.IsDigit(c) || c == '.').ToArray()), out boxWeight);

                        //  Get ItemID, if not found, keep the data zero as default from the database=========================================
                        /* int itemID = 0;
                         string getItemQuery = "SELECT ItemID FROM ItemMasterfile WHERE ItemDescription = @ItemDescription LIMIT 1;";
                         using (MySqlCommand cmd = new MySqlCommand(getItemQuery, conn))
                         {
                             cmd.Parameters.AddWithValue("@ItemDescription", itemDescription);
                             object result = cmd.ExecuteScalar();
                             if (result != null) itemID = Convert.ToInt32(result);
                         }*/
                        int? itemID = null;
                        string itemDescriptionText = itemDescription;

                        string getItemQuery = "SELECT ItemID FROM ItemMasterfile WHERE ItemDescription = @ItemDescription LIMIT 1;";
                        using (MySqlCommand cmd = new MySqlCommand(getItemQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@ItemDescription", itemDescription);
                            object result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                itemID = Convert.ToInt32(result);
                                itemDescriptionText = null; 
                            }
                        }

                        //====================================================================================================================

                        //  Get BoxTypeID=====================================================================================================
                        int boxTypeID = 0;
                        string getBoxQuery = "SELECT BoxTypeID FROM BoxTypeMasterfile WHERE BoxTypeDescription = @BoxTypeDesc LIMIT 1;";
                        using (MySqlCommand cmd = new MySqlCommand(getBoxQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@BoxTypeDesc", boxTypeDesc);
                            object result = cmd.ExecuteScalar();
                            if (result != null) boxTypeID = Convert.ToInt32(result);
                        }
                        //====================================================================================================================
                        // ======= isVoid detection =============================================================
                        // If any cell in the row has a set strikeout font or inherits a strikeout font,
                        // consider the row voided.
                        int isVoid = 0;
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            Font cellFont = null;

                            //Check if cell has an explicit style font set==============================================
                            if (cell.Style?.Font != null)
                                cellFont = cell.Style.Font;
                            else
                                //Fall back to the font actually used by the cell (inherited style)=====================
                                cellFont = cell.InheritedStyle?.Font;

                            if (cellFont != null && cellFont.Strikeout)
                            {
                                isVoid = 1;
                                break;
                            }
                        }
                        // ==================================================================================================================================================
                        // Update DateTimeFinished to the current date and time
                        string updateFinishedQuery = @" UPDATE TransactionMasterfile SET DateTimeFinished = @DateTimeFinished  WHERE Transaction_ID = @TransactionID;";

                        using (MySqlCommand cmd = new MySqlCommand(updateFinishedQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@DateTimeFinished", DateTime.Now);
                            cmd.Parameters.AddWithValue("@TransactionID", transactionID);
                            cmd.ExecuteNonQuery();
                        }
                        //===================================================================================================================================================
                        //  Insert into TransactionDetails including ItemNo, BoxWeight, IsVoid==========================
                        string insertDetailQuery = @"
               INSERT INTO TransactionDetails 
               (TransactionID, ItemNo, ItemID, OtherScraps, BoxTypeID, BoxWeight, Weight, IsVoid)
               VALUES (@TransactionID, @ItemNo, @ItemID, @OtherScraps, @BoxTypeID, @BoxWeight, @Weight, @IsVoid);
           ";
                        using (MySqlCommand cmd = new MySqlCommand(insertDetailQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@TransactionID", transactionID);
                            cmd.Parameters.AddWithValue("@ItemNo", itemNo);
                            //cmd.Parameters.AddWithValue("@ItemID", itemID);
                            cmd.Parameters.AddWithValue("@ItemID", itemID.HasValue ? (object)itemID.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@OtherScraps", itemDescriptionText ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BoxTypeID", boxTypeID);
                            cmd.Parameters.AddWithValue("@BoxWeight", boxWeight);
                            cmd.Parameters.AddWithValue("@Weight", weight);
                            cmd.Parameters.AddWithValue("@IsVoid", isVoid);
                            cmd.ExecuteNonQuery();
                        }

                        itemNo++; // increment this shit for the next row
                    }

                    MessageBox.Show("Transaction and details saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    allowClose = true;
                    this.Close();
                    mainMenuForm.Show();


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            ShowOverlay();
            isWeighingPaused = true;

            using (SaveTransactionScreen saveForm = new SaveTransactionScreen())
            {
                saveForm.transactionNo = transactionNo;
                saveForm.customerName = customerName;
                saveForm.TotalItems = GetTotalItems();
                saveForm.TotalNetWeight = GetTotalNetWeight();
                saveForm.TotalBoxWeight = GetTotalBoxWeight();
                saveForm.TotalGrossWeight = GetTotalGrossWeight();

                saveForm.Owner = this; 

                saveForm.ShowDialog();
            }


            isWeighingPaused = false;
            HideOverlay();
        }
        //===================================================================================================================
        private int GetTotalItems()
        {
            int totalItems = 0;
            foreach (DataGridViewRow row in dataGridView1.Rows)
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

                if (!isVoided)
                {
                    totalItems++;
                }
            }
            return totalItems;
        }
        //===================================================================================================================
        private decimal GetTotalNetWeight()
        {
            decimal totalNetWeight = 0m;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                object weightVal = row.Cells["NET Weight (KGS) - Box"].Value;
                if (weightVal != null)
                {
                    string s = new string(weightVal.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
                    decimal.TryParse(s, out decimal weight);
                    totalNetWeight += weight;
                }
            }
            return totalNetWeight;
        }
        //===================================================================================================================
        private decimal GetTotalBoxWeight()
        {
            decimal totalBoxWeight = 0m;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                object boxVal = row.Cells["Box Weight (KGS)"].Value;
                if (boxVal != null)
                {
                    string s = new string(boxVal.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
                    decimal.TryParse(s, out decimal boxWeight);
                    totalBoxWeight += boxWeight;
                }
            }
            return totalBoxWeight;
        }

        private decimal GetTotalGrossWeight()
        {
            return GetTotalNetWeight() + GetTotalBoxWeight();
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void pictureBox2_Click(object sender, EventArgs e)
        {
            ShowBaudRateWarning(2400, true);
        }
        //=================================================================
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            ShowBaudRateWarning(9600, false);
        }
        //=================================================================
        private void ShowBaudRateWarning(int baudRate, bool showPic1)
        {
           
            DialogResult result = MessageBox.Show(
                $"You are about to change the device connection to a baud rate of {baudRate}.\n\n" +
                "Changing the device may interrupt communication.\n" +
                "Do you want to continue?",
                "Confirm Scale Device Change",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.OK)
            {
              
                ChangeBaudRate(baudRate);
                SetPictureVisibility(showPic1);

            }
            else
            {

            }
        }
        //=================================================================
        private void SetPictureVisibility(bool showPic1)
        {
            panel3.Visible = !showPic1;
            panel1.Visible = showPic1;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        // isolate these voids for future program use
        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e) { }
        private void richTextBox5_TextChanged(object sender, EventArgs e) { }
        private void richTextBox4_TextChanged(object sender, EventArgs e) { }
        private void richTextBox2_TextChanged(object sender, EventArgs e) { }
        private void richTextBox3_TextChanged(object sender, EventArgs e) { }
        private void richTextBox1_TextChanged(object sender, EventArgs e) { }
        private void tabPage1_Click(object sender, EventArgs e){ }
        private void tabPage2_Click(object sender, EventArgs e){ }
        private void DM2pictureBox1_Click(object sender, EventArgs e){ }
        private void FI03pictureBox4_Click(object sender, EventArgs e){ }
        private void FI03Fujipanel3_Paint(object sender, PaintEventArgs e){ }
        private void FujiDM2panel1_Paint(object sender, PaintEventArgs e){ }
        private void richTextBox9_TextChanged(object sender, EventArgs e){ }
        private void richTextBox6_TextChanged_2(object sender, EventArgs e) { }
        private void TaredtoZeroIndicator_richTextBox7_TextChanged(object sender, EventArgs e) { }
        private void panel2_Paint(object sender, PaintEventArgs e) { }
        private void label1_Click_1(object sender, EventArgs e) { }
        private void richTextBox8_TextChanged(object sender, EventArgs e){ }
    }
}
