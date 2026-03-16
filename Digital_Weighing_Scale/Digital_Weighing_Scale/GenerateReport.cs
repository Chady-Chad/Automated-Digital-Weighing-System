using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.Windows.Forms.DataVisualization.Charting;
using System.Configuration;

namespace Digital_Weighing_Scale
{
    //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public partial class GenerateReport : Form
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConnection"].ConnectionString;
        //  string connectionString = "server=10.0.253.60;user=root;password=Windows7;database=Scraps";
        //  string connectionString = "server=localhost;user=root;password=Windows7;database=Scraps";
        // string connectionString = "server=localhost;user=root;password=masterx;database=Scraps";


        public GenerateReport()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            chart1.ChartAreas[0].AxisX.Title = "Company";
            chart1.ChartAreas[0].AxisY.Title = "Total Scraps (kg)";
            chart1.Series.Clear();
            chart1.Series.Add("Scraps");
            chart1.Series["Scraps"].ChartType = SeriesChartType.Column;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DesignDataGridView();
            InitializeDataGridViewColumns();

            this.KeyPreview = true;
            this.KeyDown += dataGridView1ScrollControl;

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
        private void GenerateReport_Load(object sender, EventArgs e)
        {

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void LoadReport()
        {
            DateTime startDate = dateTimePicker1.Value.Date;
            DateTime endDate = dateTimePicker2.Value.Date;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // TOTAL TRANSACTIONS (excluding cancelled transactions)
                MySqlCommand cmdCount = new MySqlCommand(
                    @"SELECT COUNT(*) 
              FROM TransactionMasterfile tm
              WHERE tm.TransactionDate BETWEEN @start AND @end 
                AND tm.IsCancel = 0
                AND NOT EXISTS (
                    SELECT 1 FROM CancelledTransaction ct 
                    WHERE ct.TransactionID = tm.Transaction_ID
                )", conn);

                cmdCount.Parameters.AddWithValue("@start", startDate);
                cmdCount.Parameters.AddWithValue("@end", endDate);
                int totalTransactions = Convert.ToInt32(cmdCount.ExecuteScalar());
                label1.Text = $"Total Transactions: {totalTransactions}";


                // TOTAL SCRAPS (exclude IsVoid = 1)
                MySqlCommand cmdScraps = new MySqlCommand(
                    @"SELECT c.CustomerName, SUM(td.Weight) AS TotalScraps
              FROM TransactionMasterfile tm
              JOIN TransactionDetails td ON tm.Transaction_ID = td.TransactionID
              JOIN CustomerMasterfile c ON tm.CustomerID = c.Customer_ID
              WHERE tm.TransactionDate BETWEEN @start AND @end
                AND tm.IsCancel = 0
                AND td.IsVoid = 0
                AND NOT EXISTS (
                    SELECT 1 FROM CancelledTransaction ct
                    WHERE ct.TransactionID = tm.Transaction_ID
                )
              GROUP BY c.CustomerName", conn);

                cmdScraps.Parameters.AddWithValue("@start", startDate);
                cmdScraps.Parameters.AddWithValue("@end", endDate);

                DataTable dt = new DataTable();
                dt.Load(cmdScraps.ExecuteReader());
                dataGridView1.DataSource = dt;


                chart1.Series["Scraps"].Points.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    chart1.Series["Scraps"].Points.AddXY(row["CustomerName"], row["TotalScraps"]);
                }

                // Total scraps value
                double totalScrapWeight = 0;
                foreach (DataRow row in dt.Rows)
                    totalScrapWeight += Convert.ToDouble(row["TotalScraps"]);

                label2.Text = $"Total Scraps: {totalScrapWeight} kg";
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void DesignDataGridView()
        {
            
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.AllowUserToAddRows = false; 
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.RowTemplate.Height = 35; 
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.GridColor = Color.LightGray;
            dataGridView1.EnableHeadersVisualStyles = false;

          
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

         
            dataGridView1.RowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.RowsDefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.RowsDefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.RowsDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

          
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(236, 240, 241);

          
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 128, 185); 
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

          
            dataGridView1.RowHeadersVisible = false;

        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void InitializeDataGridViewColumns()
        {
          
            dataGridView1.Columns.Clear();

            
            DataGridViewTextBoxColumn customerColumn = new DataGridViewTextBoxColumn();
            customerColumn.Name = "CustomerName";
            customerColumn.HeaderText = "Customer Name";
            customerColumn.DataPropertyName = "CustomerName"; 
            customerColumn.ReadOnly = true;
            dataGridView1.Columns.Add(customerColumn);

         
            DataGridViewTextBoxColumn totalScrapsColumn = new DataGridViewTextBoxColumn();
            totalScrapsColumn.Name = "TotalScraps";
            totalScrapsColumn.HeaderText = "Total Scraps (kg)";
            totalScrapsColumn.DataPropertyName = "TotalScraps"; 
            totalScrapsColumn.ReadOnly = true;
            dataGridView1.Columns.Add(totalScrapsColumn);
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            LoadReport();
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //isolate these voids for future functions
        private void chart1_Click(object sender, EventArgs e){ }
        private void StartdateTimePicker1_ValueChanged(object sender, EventArgs e){ }
        private void label1_Click(object sender, EventArgs e){ }
        private void EnddateTimePicker2_ValueChanged(object sender, EventArgs e){ }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
