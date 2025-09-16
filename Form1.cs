using System;
using System.Diagnostics;


namespace _9sMenu
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        private Dictionary<string, Process> runningProcesses = new Dictionary<string, Process>();

        public Form1()
        {
            InitializeComponent();
            InitializeTimer();
            InitializeAccountList();

            this.FormClosing += YourForm_FormClosing;
        }

        private void YourForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 關閉所有正在執行的程序
            CloseAllRunningProcesses();
        }

        private void CloseAllRunningProcesses()
        {
            foreach (var kvp in runningProcesses.ToList()) // 使用 ToList() 避免修改集合時的問題
            {
                try
                {
                    Process process = kvp.Value;
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"關閉程序時發生錯誤: {ex.Message}");
                }
            }

            // 清空記錄
            runningProcesses.Clear();
        }

        private void InitializeAccountList()
        {
            try
            {
                // 設定等寬字體
                listBox2.Font = new Font("Consolas", 9);

                string[] lines = File.ReadAllLines("accounts.txt");
                listBox2.Items.Clear(); // 清空現有項目

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3)
                    {
                        // 格式化顯示，讓縮排一致
                        string formatted = $"{parts[0],-3} {parts[1],-15} {parts[2]}";
                        listBox2.Items.Add(formatted);
                    }
                    else
                    {
                        // 如果格式不正確，仍然顯示原始行
                        listBox2.Items.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取帳戶檔案錯誤: {ex.Message}");
            }
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
                    listBox1.Items.Add($"{process.MainWindowTitle} {process.Id.ToString()}");
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

        

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                // 獲取 ListBox 中選中的項目（它是字串格式的 PID）
                string selectedPid = listBox1.SelectedItem.ToString();
                textBox1.Text = selectedPid;
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtonState();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // 指定要開啟的 exe 檔案路徑
                string exePath = @"C:\Users\user\FlashGameLoader\bin\Debug\net8.0-windows\9s.exe";
                string selectedValue = listBox2.SelectedItem.ToString();
                string id = selectedValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

                Process process = Process.Start(exePath, $"-i {id}");

                runningProcesses[id] = process;

                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) => {
                    // 程序結束時移除記錄
                    if (runningProcesses.ContainsKey(id))
                    {
                        runningProcesses.Remove(id);
                    }

                    // 更新UI（需要跨線程調用）
                    this.Invoke(new Action(() => {
                        UpdateButtonState();
                    }));
                };
                UpdateButtonState();

            }
            catch (Exception ex)
            {
                MessageBox.Show("無法開啟程式: " + ex.Message);
            }
        }

        private void UpdateButtonState()
        {
            if (listBox2.SelectedItem == null)
            {
                button1.Enabled = false;
                return;
            }

            string selectedValue = listBox2.SelectedItem.ToString();
            string id = selectedValue.Split(' ')[0];

            // 檢查這個帳戶是否正在使用中
            bool isRunning = runningProcesses.ContainsKey(id) &&
                            !runningProcesses[id].HasExited;

            if (isRunning)
            {
                button1.Enabled = false;
                button1.BackColor = Color.Orange;
                button1.Text = "使用中";
            }
            else
            {
                button1.Enabled = true;
                button1.BackColor = SystemColors.Control;
                button1.Text = "開啟";
            }
        }

    }
}
