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
            // �����Ҧ����b���檺�{��
            CloseAllRunningProcesses();
        }

        private void CloseAllRunningProcesses()
        {
            foreach (var kvp in runningProcesses.ToList()) // �ϥ� ToList() �קK�קﶰ�X�ɪ����D
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
                    Console.WriteLine($"�����{�Ǯɵo�Ϳ��~: {ex.Message}");
                }
            }

            // �M�ŰO��
            runningProcesses.Clear();
        }

        private void InitializeAccountList()
        {
            try
            {
                // �]�w���e�r��
                listBox2.Font = new Font("Consolas", 9);

                string[] lines = File.ReadAllLines("accounts.txt");
                listBox2.Items.Clear(); // �M�Ų{������

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3)
                    {
                        // �榡����ܡA���Y�Ƥ@�P
                        string formatted = $"{parts[0],-3} {parts[1],-15} {parts[2]}";
                        listBox2.Items.Add(formatted);
                    }
                    else
                    {
                        // �p�G�榡�����T�A���M��ܭ�l��
                        listBox2.Items.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ū���b���ɮ׿��~: {ex.Message}");
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

                // ���o��e�i�{�C��A�ÿz�� ProcessName �� "9s" ���i�{
                var currentProcesses = Process.GetProcesses()
                    .Where(p => p.ProcessName.Equals("9s", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(p => p.Id, p => p.MainWindowTitle);

                var currentPids = currentProcesses.Keys.ToHashSet();

                // �����w���s�b�� PID
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

                // �K�[�s�X�{�� PID
                foreach (int pid in currentPids)
                {
                    string item = $"{currentProcesses[pid]} {pid}";
                    if (!listBox1.Items.Cast<string>().Contains(item))
                    {
                        listBox1.Items.Add(item);
                    }
                }

                // ��_������A
                if (selectedPid != -1)
                {
                    string selectedItem = listBox1.Items.Cast<string>().FirstOrDefault(item => item.EndsWith($" {selectedPid}"));
                    if (selectedItem != null)
                    {
                        listBox1.SelectedItem = selectedItem;
                    }
                    else
                    {
                        LogMessage($"����� PID {selectedPid} �w���s�b");
                        lastSelectedPid = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"��s�i�{�C����: {ex.Message}");
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
            richTextBox1.Multiline = true;                             //��ܦh��
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Vertical;�@//�u��ܫ����u��
        }



        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                // ��� ListBox ���襤�����ء]���O�r��榡�� PID�^
                string[] selectedPid = listBox1.SelectedItem.ToString().Split(' ');
                textBox1.Text = selectedPid[1];
                LogMessage($"��� PID: {selectedPid[1]}");

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
                // ���w�n�}�Ҫ� exe �ɮ׸��|
                string exePath = @"C:\Users\user\FlashGameLoader\bin\Debug\net8.0-windows\9s.exe";
                string selectedValue = listBox2.SelectedItem.ToString();
                string id = selectedValue.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

                Process process = Process.Start(exePath, $"-i {id}");

                runningProcesses[id] = process;

                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) =>
                {
                    // �{�ǵ����ɲ����O��
                    if (runningProcesses.ContainsKey(id))
                    {
                        runningProcesses.Remove(id);
                    }

                    // ��sUI�]�ݭn��u�{�եΡ^
                    this.Invoke(new Action(() =>
                    {
                        UpdateButtonState();
                    }));
                };
                UpdateButtonState();

            }
            catch (Exception ex)
            {
                MessageBox.Show("�L�k�}�ҵ{��: " + ex.Message);
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

            // �ˬd�o�ӱb��O�_���b�ϥΤ�
            bool isRunning = runningProcesses.ContainsKey(id) &&
                            !runningProcesses[id].HasExited;

            if (isRunning)
            {
                button1.Enabled = false;
                button1.BackColor = Color.Orange;
                button1.Text = "�ϥΤ�";
            }
            else
            {
                button1.Enabled = true;
                button1.BackColor = SystemColors.Control;
                button1.Text = "�}��";
            }
        }


        // ��@�{�Ǫ`�J
        private void button2_Click_1(object sender, EventArgs e)
        {   

            if (listBox1.SelectedIndex == -1)
            {
                LogMessage("����ܭn�`�J��PID");
            }
            else
            {
                string selectedValue = listBox1.SelectedItem.ToString();
                // ��PID list���ĤG�ӼƭȡA�]�N�OPID
                int pid = int.Parse(selectedValue.Split(' ')[1]);
                bool success = DllInjector.InjectDll(pid, dllPath, LogMessage);
                MessageBox.Show(success ? "DLL �`�J���\�I" : "DLL �`�J���ѡI",
                    success ? "���\" : "���~",
                    MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
                
        }

        // ���log
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
                LogMessage("����ܭn�����`�J��PID");
            }
            else
            {
                string selectedValue = listBox1.SelectedItem.ToString();
                // ��PID list���ĤG�ӼƭȡA�]�N�OPID
                int pid = int.Parse(selectedValue.Split(' ')[1]);
                bool success = DllInjector.EjectDll(pid, dllPath, LogMessage);
                MessageBox.Show(success ? "DLL �������\�I" : "DLL �������ѡI",
                    success ? "���\" : "���~",
                    MessageBoxButtons.OK,
                    success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
                
        }
    }
}
