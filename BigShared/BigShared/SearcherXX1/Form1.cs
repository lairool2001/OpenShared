using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SearcherXX1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string search = richTextBox1.Text;
            richTextBox2.Clear();
            richTextBox2.AppendText("搜尋中，請稍候...\n");
            Task.Run(() =>
            {
                DateTime start = DateTime.Now;
                DoJob(search);
                DateTime end = DateTime.Now;
                Invoke(new Action(() =>
                {
                    richTextBox2.AppendText($"\n搜尋完成，耗時 {(end - start).TotalSeconds} 秒");
                }));

            });
        }

        private async Task DoJob(string search)
        {
            // 取得所有硬碟資訊
            DriveInfo[] drives = DriveInfo.GetDrives();

            // 組裝結果的本地容器，最後一次性放到 UI
            var results = new ConcurrentBag<string>();

            Parallel.ForEach(drives, d =>
            {
                if (d.DriveFormat == "NTFS")
                {
                    return;
                }
                //foreach (var d in drives)
                //{
                try
                {
                    // 先寫入盤名
                    //results.Add($"Name: {d.Name}");

                    // 取得該磁碟根目錄的檔案清單，這裡用安全的例外處理
                    string[] rootFiles = Directory.GetFiles(d.Name);
                    Parallel.ForEach(rootFiles, file =>
                    {
                        if (file.Contains(search))
                        {
                            results.Add(file);
                        }
                    });

                    // 以堆疊方式遞迴掃描子目錄，避免同時修改同一併發集合
                    var stack = new ConcurrentStack<string>();
                    stack.Push(d.Name);

                    while (stack.Count > 0)
                    {
                        stack.TryPop(out var current);
                        string[] dirs = null;
                        string[] files = null;

                        try
                        {
                            dirs = Directory.GetDirectories(current);
                            Parallel.ForEach(dirs, dir =>
                            {
                                stack.Push(dir);
                                if (dir.Contains(search) && !results.Contains(dir))
                                {
                                    results.Add(dir);
                                }
                            });
                        }
                        catch
                        {
                            // 忽略無法存取的目錄/檔案
                        }
                        try
                        {
                            files = Directory.GetFiles(current);
                            Parallel.ForEach(files, file =>
                            {
                                file = file.Replace("\r", "");
                                if (file.Contains(search) && !results.Contains(file))
                                {
                                    results.Add(file);
                                }
                            });
                        }
                        catch
                        {
                            // 忽略無法存取的目錄/檔案
                        }

                    }
                }
                catch
                {
                    // 忽略無法存取的磁碟
                }
            });
            //
            Invoke(new Action(() =>
            {
                richTextBox2.Clear();
                // 將結果轉成單一字串後顯示
                foreach (var item in results)
                {
                    InsertFileLink(richTextBox2, item);
                }
                //richTextBox2.ScrollToCaret();
            }));
        }

        private void richTextBox2_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            string link = e.LinkText.Replace("\r", "");
            for (int i = 0; i < richTextBox2.Lines.Length; i++)
            {
                string line = richTextBox2.Lines[i];
                if(line==link)
                {
                    richTextBox2.Select(richTextBox2.GetFirstCharIndexFromLine(i), line.Length);
                    break;
                }
            }
            Enabled = false;
            // --- 做連接或內容新增 ---
            try
            {
                // Use Process.Start to open the linked file
                System.Diagnostics.Process.Start(link);
                Focus();
            }
            catch (Exception ex)
            {
                // Handle any potential errors, e.g., file not found
                MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            timer1.Start();
        }

        /// <summary>
        /// 在指定的 RichTextBox 末端插入一個檔案路徑的超連結（顯示文字預設為完整路徑）。
        /// 呼叫者應在 UI 執行緒使用此方法。
        /// </summary>
        private void InsertFileLink(RichTextBox rtb, string filePath, string displayText = null)
        {
            if (rtb == null) throw new ArgumentNullException(nameof(rtb));
            if (string.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            // 顯示文字預設為完整路徑
            if (string.IsNullOrEmpty(displayText)) displayText = filePath;
            displayText = displayText.Replace("\r", "");
            // 關閉自動偵測網址，避免干擾自訂 Link 樣式
            rtb.DetectUrls = false;
            // 記住插入位置並加入文字
            int start = rtb.TextLength;
            rtb.AppendText(displayText);
            rtb.AppendText("\n\n");

            // 選取剛加入的文字範圍
            rtb.Select(start, displayText.Length);

            // 設定為 Link (使用 EM_SETCHARFORMAT / CHARFORMAT2)
            var cf = new CHARFORMAT2_STRUCT();
            cf.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(CHARFORMAT2_STRUCT));
            cf.dwMask = CFM_LINK;
            cf.dwEffects = CFE_LINK;

            SendMessage(rtb.Handle, EM_SETCHARFORMAT, new IntPtr(SCF_SELECTION), ref cf);
        }

        // Win32 常數與結構
        private const int EM_SETCHARFORMAT = 0x0444;
        private const int SCF_SELECTION = 0x0001;
        private const uint CFM_LINK = 0x00000020;
        private const uint CFE_LINK = 0x00000020;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct CHARFORMAT2_STRUCT
        {
            public uint cbSize;
            public uint dwMask;
            public uint dwEffects;
            public int yHeight;
            public int yOffset;
            public int crTextColor;
            public byte bCharSet;
            public byte bPitchAndFamily;
            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szFaceName;
            public ushort wWeight;
            public short sSpacing;
            public int crBackColor;
            public uint lcid;
            public uint dwReserved;
            public short sStyle;
            public short wKerning;
            public byte bUnderlineType;
            public byte bAnimation;
            public byte bRevAuthor;
            public byte bReserved1;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref CHARFORMAT2_STRUCT cf);

        private void richTextBox2_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void richTextBox2_VScroll(object sender, EventArgs e)
        {
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Enabled = true;
            Focus();
            timer1.Stop();
        }

        private void richTextBox2_MouseDown(object sender, MouseEventArgs e)
        {
        }
    }
}
