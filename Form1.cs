using System.Diagnostics;


namespace _9sMenu
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        public Form1()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1000 milliseconds = 1 second
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateProcessList();
        }

        private void UpdateProcessList()
        {
            listBox1.Items.Clear(); // Clear the existing items

            // Get all processes
            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                // Check if the process name is "9s"
                if (process.ProcessName.Equals("9s", StringComparison.OrdinalIgnoreCase))
                {
                    // Add the process ID to the ListBox
                    listBox1.Items.Add(process.Id.ToString());
                }
            }
        }

        // Make sure to dispose of the timer when the form is closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.OnFormClosing(e);
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 指定要開啟的 exe 檔案路徑
                string exePath = @"C:\Users\user\FlashGameLoader\bin\Debug\net8.0-windows\9s.exe";

                Process.Start(exePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("無法開啟程式: " + ex.Message);
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
