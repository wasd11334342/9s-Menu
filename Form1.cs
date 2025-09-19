using DllInjectorExample;
using System;
using System.Diagnostics;
using System.Security.Cryptography;


namespace _9sMenu
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer timer;
        private Dictionary<string, Process> runningProcesses = new Dictionary<string, Process>();
        private int lastSelectedPid = -1;
        private string dllPath = "dll_test.dll";
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
            try
            {
                int selectedPid = lastSelectedPid;

                // 取得當前進程列表，並篩選 ProcessName 為 "9s" 的進程
                var currentProcesses = Process.GetProcesses()
                    .Where(p => p.ProcessName.Equals("9s", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(p => p.Id, p => p.MainWindowTitle);

                var currentPids = currentProcesses.Keys.ToHashSet();

                // 移除已不存在的 PID
                for (int i = listBox1.Items.Count - 1; i >= 0; i--)
                {
                    string item = (string)listBox1.Items[i];
                    string[] parts = item.Split(' ');
                    if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1].Trim(), out int pid))
                    {
                        if (!currentPids.Contains(pid))
                        {
                            listBox1.Items.RemoveAt(i);
                        }
                    }
                }

                // 添加新出現的 PID
                foreach (int pid in currentPids)
                {
                    string item = $"{currentProcesses[pid]} {pid}";
                    if (!listBox1.Items.Cast<string>().Contains(item))
                    {
                        listBox1.Items.Add(item);
                    }
                }

                // 恢復選取狀態
                if (selectedPid != -1)
                {
                    string selectedItem = listBox1.Items.Cast<string>().FirstOrDefault(item => item.EndsWith($" {selectedPid}"));
                    if (selectedItem != null)
                    {
                        listBox1.SelectedItem = selectedItem;
                    }
                    else
                    {
                        LogMessage($"選取的 PID {selectedPid} 已不存在");
                        lastSelectedPid = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"更新進程列表失敗: {ex.Message}");
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
            richTextBox1.Multiline = true;                             //顯示多行
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;　//只顯示垂直滾動
        }



        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                // 獲取 ListBox 中選中的項目（它是字串格式的 PID）
                string[] selectedPid = listBox1.SelectedItem.ToString().Split(' ');
                textBox1.Text = selectedPid[1];
                LogMessage($"選取 PID: {selectedPid[1]}");

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
                process.Exited += (sender, e) =>
                {
                    // 程序結束時移除記錄
                    if (runningProcesses.ContainsKey(id))
                    {
                        runningProcesses.Remove(id);
                    }

                    // 更新UI（需要跨線程調用）
                    this.Invoke(new Action(() =>
                    {
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


        // 單一程序注入
        private void button2_Click_1(object sender, EventArgs e)
        {   

            if (listBox1.SelectedIndex == -1)
            {
                LogMessage("未選擇要注入的PID");
            }
            else
            {
                string selectedValue = listBox1.SelectedItem.ToString();
                // 用PID list的第二個數值，也就是PID
                int pid = int.Parse(selectedValue.Split(' ')[1]);
                bool success = DllInjector.InjectDll(pid, dllPath, LogMessage);
                MessageBox.Show(success ? "DLL 注入成功！" : "DLL 注入失敗！",
                    success ? "成功" : "錯誤",
                    MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
                
        }

        // 顯示log
        private void LogMessage(string message)
        {
            richTextBox1.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
            {
                LogMessage("未選擇要取消注入的PID");
            }
            else
            {
                string selectedValue = listBox1.SelectedItem.ToString();
                // 用PID list的第二個數值，也就是PID
                int pid = int.Parse(selectedValue.Split(' ')[1]);
                bool success = DllInjector.EjectDll(pid, dllPath, LogMessage);
                MessageBox.Show(success ? "DLL 卸載成功！" : "DLL 卸載失敗！",
                    success ? "成功" : "錯誤",
                    MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
                
        }
    }
}
