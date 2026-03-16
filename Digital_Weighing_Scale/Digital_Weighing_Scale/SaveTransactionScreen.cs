using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace Digital_Weighing_Scale
{
    public partial class SaveTransactionScreen : Form
    {
        [DllImport("user32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);
        private const int WM_VSCROLL = 0x0115;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        public string transactionNo { get; set; }
        public string customerName { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal TotalBoxWeight { get; set; }
        public decimal TotalGrossWeight { get; set; }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public SaveTransactionScreen()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.KeyDown += SaveAndPrint;
            this.KeyDown += cancelButton;
            this.KeyDown += RichTextBox3ScrollControl;
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //=============================functions for KEyboard control on top==================================================
        private void SaveAndPrint(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!button3.Enabled)
                {
                    MessageBox.Show(
                        "You cannot save this transaction because there are no valid items.",
                        "Save Blocked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
                button3_Click(sender, e);
            }
        }
        //=========================================================
        private void cancelButton(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                button2_Click(sender, e);
                this.Close();
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        /* private void button3_Click(object sender, EventArgs e)
         {

             TransactionScreen parentTransactionForm = this.Owner as TransactionScreen;
             if (parentTransactionForm != null)
             {
                 parentTransactionForm.SaveTransactionToDatabase();
                 this.Close();
             }
             else
             {
                 MessageBox.Show("Parent transaction form not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
             }
         }*/
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button3_Click(object sender, EventArgs e)
        {
       
            if (ConfirmSaveTransaction())
            {
                TransactionScreen parentTransactionForm = this.Owner as TransactionScreen;
                if (parentTransactionForm != null)
                {
                    parentTransactionForm.SaveTransactionToDatabase();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Parent transaction form not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool ConfirmSaveTransaction()
        {
            string message = "Are you sure you want to save this transaction? Please review all the details carefully before proceeding.";
            string caption = "Confirm Save";
            DialogResult result = MessageBox.Show(message, caption, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);

            return result == DialogResult.OK;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //This is backup function incase there is a sudden changes or problem
        /*private void SaveTransactionScreen_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = $"Transaction No: {transactionNo}";
            richTextBox2.Text = $"Customer: {customerName}";

            TransactionScreen parentTransactionForm = this.Owner as TransactionScreen;

            if (parentTransactionForm != null)
            {
                richTextBox3.Text = parentTransactionForm.TotalWeightsText;
            }
            else
            {
              
                richTextBox3.Text = $"Total Items: {TotalItems}\n" +
                                    $"Total Net Weight: {TotalNetWeight:0.00} KGS\n" +
                                    $"Total Box Weight: {TotalBoxWeight:0.00} KGS\n" +
                                    $"Total Gross Weight: {TotalGrossWeight:0.00} KGS";
                                    
            }
            string itemList = parentTransactionForm.GetItemList();
            richTextBox3.AppendText($"\n\nList of Items: \n{itemList}");
        }
        */
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void SaveTransactionScreen_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = $"Transaction No: {transactionNo}";
            richTextBox2.Text = $"Customer: {customerName}";

            TransactionScreen parentTransactionForm = this.Owner as TransactionScreen;

           
            if (parentTransactionForm != null)
            {
                richTextBox3.Text = parentTransactionForm.TotalWeightsText;
            }
            else
            {
                richTextBox3.Text = $"Total Items: {TotalItems}\n" +
                                    $"Total Net Weight: {TotalNetWeight:0.00} KGS\n" +
                                    $"Total Box Weight: {TotalBoxWeight:0.00} KGS\n" +
                                    $"Total Gross Weight: {TotalGrossWeight:0.00} KGS";
            }

          
            string itemList = parentTransactionForm.GetItemList();

            if (string.IsNullOrWhiteSpace(itemList))
            {
             
                MessageBox.Show(
                    "Warning: No items have been added to the list, or the items have been excluded, leaving no valid entries.\n" +
                    "You cannot save this transaction until there is at least one valid item.",
                    "No Valid Items",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

           
                button3.Enabled = false;
            }
            else
            {
                richTextBox3.AppendText($"\n\nList of Items: \n{itemList}");
                button3.Enabled = true; 
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void TransactionNorichTextBox1_TextChanged(object sender, EventArgs e){
            richTextBox1.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            richTextBox1.ForeColor = Color.DarkBlue;
            richTextBox1.BackColor = Color.AliceBlue;
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center; 
            richTextBox1.ScrollBars = RichTextBoxScrollBars.None;
            HideCaret(richTextBox1.Handle);
        }
        private void CustomerNamerichTextBox2_TextChanged(object sender, EventArgs e){
            richTextBox2.Font = new Font("Segoe UI", 20, FontStyle.Italic);
            richTextBox2.ForeColor = Color.DarkGreen;
            richTextBox2.BackColor = Color.Lavender;
            richTextBox2.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox2.ScrollBars = RichTextBoxScrollBars.None;
            HideCaret(richTextBox2.Handle);
        }
        private void richTextBox3_TextChanged(object sender, EventArgs e){
            richTextBox3.Font = new Font("Consolas", 13, FontStyle.Regular); 
            richTextBox3.ForeColor = Color.Black;
            richTextBox3.BackColor = Color.LightYellow;
            richTextBox3.SelectionAlignment = HorizontalAlignment.Left; 
            richTextBox3.ScrollBars = RichTextBoxScrollBars.Vertical;
            HideCaret(richTextBox3.Handle);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void RichTextBox3ScrollControl(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                // Scroll up
                SendMessage(richTextBox3.Handle, WM_VSCROLL, (IntPtr)SB_LINEUP, IntPtr.Zero);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                // Scroll down
                SendMessage(richTextBox3.Handle, WM_VSCROLL, (IntPtr)SB_LINEDOWN, IntPtr.Zero);
                e.Handled = true;
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}
