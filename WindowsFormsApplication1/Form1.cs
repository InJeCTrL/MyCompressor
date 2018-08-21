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
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
namespace MyCompressor
{
    public partial class Form1 : Form
    {
        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess([In] IntPtr processHandle);
        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess([In] IntPtr processHandle);

        Process proc;//新建的ffmpeg进程
        Thread th;//用于创建并监视proc的线程
        int tDuration;//临时存储视频时长
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false; 
        }
        private void ComPressor()
        {/*创建ffmpeg进程并监视*/
            for (int i = 0; i < listView1.Items.Count; i++)
            {//列表从上之下进行压缩
                string path = listView1.Items[i].SubItems[0].Text;//临时存储预压缩的文件路径
                if (File.Exists("E:\\A\\" + Path.GetFileName(path)))//目标文件已存在
                {//检测E:\A\下是否已存在同名文件，若存在则询问覆盖或跳过
                    if (DialogResult.No == MessageBox.Show(null, "文件已经存在于输出文件夹中\n是：覆盖生成，否：跳过此文件", "文件重复", MessageBoxButtons.YesNo))
                    {//用户选择跳过，标记为complete并执行下一条压缩
                        listView1.Items[i].SubItems[1].Text = "Complete";
                        listView1.Items[i].BackColor = Color.LawnGreen;
                        continue;
                    }
                    else//用户选择覆盖，删除原有同名文件
                        File.Delete("E:\\A\\" + Path.GetFileName(path));
                }
                proc = new Process();//初始化进程
                proc.StartInfo.FileName = "ffmpeg.exe";//目标为同目录下的ffmpeg
                proc.StartInfo.UseShellExecute = false;//不使用shell启动
                proc.StartInfo.CreateNoWindow = true;//静默无窗体显示
                proc.StartInfo.RedirectStandardError = true;//重定向标准错误输出流
                proc.StartInfo.Arguments = "-i " + path + //输入path
                                     " -vcodec h264 -s 480*360 -b:v 384k " + //视频部分转换为h264 x360 动态码率 平均384k
                                     "E:\\A\\" + Path.GetFileName(path);//输出为E:\A\下同名文件
                proc.ErrorDataReceived += new DataReceivedEventHandler(FFmpeg_Output);//输出流附加到FFmpeg_Output上
                proc.Start();//启动ffmpeg进程，开始转换
                listView1.Items[i].SubItems[1].Text = "Compressing";//并标记本条状态为压缩中
                listView1.Items[i].BackColor = Color.Gold;
                proc.BeginErrorReadLine();//开始读取错误输出流
                proc.WaitForExit();//等待ffmpeg进程退出
                proc.Close();//关闭进程
                proc.Dispose();//清理进程
                listView1.Items[i].SubItems[1].Text = "Complete";//标记本条转换状态为结束
                listView1.Items[i].BackColor = Color.LawnGreen;
            }
            button1.Enabled = true;
            button1.Visible = true;
            progressBar1.Visible = false;
            button2.Enabled = false;
            button2.Text = "暂停";
            button3.Enabled = false;//各项控件恢复正常
        }
        private int GetDuration(string Line)
        {/*返回获得的视频时长*/
            string regStr = @"Duration: (\d+):(\d+):(\d+)";//正则匹配Duration: 00:00:00
            Match MatchResult = Regex.Match(Line, regStr);
            return int.Parse(MatchResult.Groups[1].Value) * 3600 +
                   int.Parse(MatchResult.Groups[2].Value) * 60+
                   int.Parse(MatchResult.Groups[3].Value);
        }
        private int GetNowStatus(string Line)
        {/*返回获得的当前转换进度*/
            string regStr = @"time=(\d+):(\d+):(\d+)";//正则匹配time=00:00:00
            Match MatchResult = Regex.Match(Line, regStr);
            return int.Parse(MatchResult.Groups[1].Value) * 3600 +
                   int.Parse(MatchResult.Groups[2].Value) * 60 +
                   int.Parse(MatchResult.Groups[3].Value);
        }
        private void FFmpeg_Output(object sender, DataReceivedEventArgs e)
        {/*处理ffmpeg重定向过程*/
            if (e != null && e.Data != null)
            {
                if (e.Data.Contains("Duration"))//若某行包含Duration字样代表该行记录时长
                    tDuration = GetDuration(e.Data);
                else if (e.Data.Contains("time="))//若某行包含time=字样代表该行为转换输出行
                    progressBar1.Value = GetNowStatus(e.Data) * 100 / tDuration;//计算转换进度并更新进度条
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {

            button1.Enabled = false;
            button1.Visible = false;
            progressBar1.Visible = true;
            button2.Enabled = true;
            button2.Text = "暂停";
            button3.Enabled = true;
            th = new Thread(ComPressor);
            th.IsBackground = true;
            th.Start();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("暂停"))
            {
                NtSuspendProcess(proc.Handle);
                for (int i = 0; i < listView1.Items.Count; i++)
                    if (listView1.Items[i].SubItems[1].Text.Equals("Compressing"))
                    {
                        listView1.Items[i].SubItems[1].Text = "Pause";
                        listView1.Items[i].BackColor = Color.Red;
                    }
                button2.Text = "继续";
            }
            else
            {
                NtResumeProcess(proc.Handle);
                for (int i = 0; i < listView1.Items.Count; i++)
                    if (listView1.Items[i].SubItems[1].Text.Equals("Pause"))
                    {
                        listView1.Items[i].SubItems[1].Text = "Compressing";
                        listView1.Items[i].BackColor = Color.Gold;
                    }
                button2.Text = "暂停";
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            th.Abort();
            proc.Kill();
            button1.Enabled = true;
            button1.Visible = true;
            progressBar1.Visible = false;
            button2.Enabled = false;
            button2.Text = "暂停";
            button3.Enabled = false;
            listView1.Enabled = true;
            for (int i = 0; i < listView1.Items.Count; i++)
                if (listView1.Items[i].SubItems[1].Text.Equals("Compressing") ||
                    listView1.Items[i].SubItems[1].Text.Equals("Pause"))
                {
                    listView1.Items[i].SubItems[1].Text = "Ready";
                    listView1.Items[i].BackColor = Color.Red;
                }
        }
        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            String[] Files = (String[])e.Data.GetData(DataFormats.FileDrop);
            for (int i = 0; i < Files.Length; i++)
            {
                if (System.IO.Path.GetExtension(Files[i]) == ".mp4")
                {
                    listView1.Items.Add(new ListViewItem(new string[] { Files[i], "Ready" }));
                    listView1.Items[listView1.Items.Count-1].BackColor = Color.Red;
                }
            }
        }
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (button1.Enabled == true && e.Data.GetData(DataFormats.FileDrop) != null)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listView1.Items.IndexOf(listView1.FocusedItem);
            if (button1.Enabled == true && index != -1)
                listView1.Items.RemoveAt(index);
        }
    }
}
