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
        Thread TaskQueue;//用于创建并监视proc的线程
        string OutputFolder;//指定输出文件夹
        int tDuration;//临时存储视频时长
        int ActiveItem;//标记当前正在处理的项目下标

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;//取消线程安全
        }
        private void Wnd_AllEnd()
        {/*转换全部终止后对窗体中各控件的操作*/
            this.Text = "MyCompressor";
            button1.Enabled = true;//禁用开始按钮
            button1.Visible = true;//隐藏开始按钮
            progressBar1.Visible = false;//显示进度条
            button2.Enabled = false;//启用暂停按钮
            button2.Text = "暂停";//暂停按钮复位
            button3.Enabled = false;//启用停止按钮
        }
        private void SetPriorityByTrackBar()
        {/*根据TrackBar的值设置FFmpeg进程与重定向处理线程的优先级*/
            switch (trackBar1.Value)
            {/*根据trackBar获取优先级设置信息*/
                case 0:
                    proc.PriorityClass = ProcessPriorityClass.Idle;//设置FFmpeg进程优先级为低
                    break;
                case 1:
                    proc.PriorityClass = ProcessPriorityClass.Normal;//设置FFmpeg进程优先级为标准
                    break;
                case 2:
                    proc.PriorityClass = ProcessPriorityClass.High;//设置FFmpeg进程优先级为高
                    break;
            }
        }
        private void ComPressor()
        {/*创建ffmpeg进程并监视*/
            for (ActiveItem = 0; ActiveItem < listView1.Items.Count; ActiveItem++)
            {//列表从上之下进行压缩
                string path = listView1.Items[ActiveItem].SubItems[0].Text;//临时存储预压缩的文件路径
                this.Text = ActiveItem + " / " + listView1.Items.Count;//设置标题栏标记总进度
                if (File.Exists(OutputFolder + "/" + Path.GetFileName(path)))//目标文件已存在
                {//检测输出文件夹下是否已存在同名文件，若存在则询问覆盖或跳过
                    if (DialogResult.No == MessageBox.Show(null, "文件已经存在于输出文件夹中\n是：覆盖生成，否：跳过此文件", "文件重复", MessageBoxButtons.YesNo))
                    {//用户选择跳过，标记为0:0:0并执行下一条压缩
                        listView1.Items[ActiveItem].SubItems[1].Text = "0:0:0";
                        listView1.Items[ActiveItem].BackColor = Color.LawnGreen;
                        continue;
                    }
                    else//用户选择覆盖，删除原有同名文件
                        File.Delete(OutputFolder + "/" + Path.GetFileName(path));
                }
                proc = new Process();//初始化进程
                proc.StartInfo.FileName = "ffmpeg.exe";//目标为同目录下的ffmpeg
                proc.StartInfo.UseShellExecute = false;//不使用shell启动
                proc.StartInfo.CreateNoWindow = true;//静默无窗体显示
                proc.StartInfo.RedirectStandardError = true;//重定向标准错误输出流
                proc.StartInfo.Arguments = "-i \"" + path + "\"" + //输入path，并加引号防止异常
                                           " -vcodec libx264 -s 480*360 -b:v 384k " + //视频部分转换为h264 x360 动态码率 平均384k
                                           "\"" + OutputFolder + "\\" + Path.GetFileName(path) + "\"";//输出到指定的输出文件夹，并加引号防止异常
                proc.ErrorDataReceived += new DataReceivedEventHandler(FFmpeg_Output);//输出流附加到FFmpeg_Output上
                proc.Start();//启动ffmpeg进程，开始转换
                DateTime BeginTime = DateTime.Now;//记录开始时间
                SetPriorityByTrackBar();//设置FFmpeg进程优先值
                listView1.Items[ActiveItem].BackColor = Color.Gold;//并标记本条颜色为金黄色，表明处于压缩状态
                proc.BeginErrorReadLine();//开始读取错误输出流
                proc.WaitForExit();//等待ffmpeg进程退出
                TimeSpan TransTime = DateTime.Now - BeginTime;//计算时间长
                listView1.Items[ActiveItem].SubItems[1].Text = TransTime.Hours.ToString() + ":" +
                                                      TransTime.Minutes.ToString() + ":" +
                                                      TransTime.Seconds.ToString();//标记本条转换状态为结束
                listView1.Items[ActiveItem].BackColor = Color.LawnGreen;
            }
            if (proc != null)//当且仅当进程被创建过才关闭并清理进程，避免异常
            {
                proc.Close();//关闭进程
                proc.Dispose();//清理进程
                proc = null;
            }
            Wnd_AllEnd();//转换正常结束，各项控件重设
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
                double tVal;//记录临时计算的进度
                if (e.Data.Contains("time="))//若某行包含time=字样代表该行为转换输出行
                {
                    tVal = GetNowStatus(e.Data) * 100.0 / tDuration;//计算百分比
                    listView1.Items[ActiveItem].SubItems[1].Text = tVal.ToString("f1") + " %";//列表中状态列显示百分比
                    progressBar1.Value = (int)tVal;//计算转换进度并更新进度条
                }
                else if (e.Data.Contains("Duration"))//若某行包含Duration字样代表该行记录时长
                    tDuration = GetDuration(e.Data);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {/*按下开始按钮*/
            button1.Enabled = false;
            button1.Visible = false;//开始按钮无效并隐藏
            progressBar1.Visible = true;//显示进度条
            button2.Enabled = true;
            button2.Text = "暂停";
            button3.Enabled = true;//暂停与停止按钮有效并重设暂停按钮
            TaskQueue = new Thread(ComPressor);
            TaskQueue.IsBackground = true;//创建后台线程
            TaskQueue.Priority = ThreadPriority.Highest;//监听线程最高优先级
            TaskQueue.Start();//启动线程
        }
        private void button2_Click(object sender, EventArgs e)
        {/*按下暂停按钮，通过该按钮的Text标记当前状态*/
            if (button2.Text.Equals("暂停"))
            {//若当前是转换状态
                NtSuspendProcess(proc.Handle);//挂起FFmpeg进程
                listView1.Items[ActiveItem].SubItems[1].Text = "Pause";
                listView1.Items[ActiveItem].BackColor = Color.Red;//在列表中标记当前活动的项目为暂停
                button2.Text = "继续";//更新按钮标记
            }
            else
            {//若当前是暂停状态
                NtResumeProcess(proc.Handle);//恢复FFmpeg进程
                listView1.Items[ActiveItem].BackColor = Color.Gold;
                button2.Text = "暂停";//更新按钮标记
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {/*按下停止按钮*/
            TaskQueue.Abort();//终止监听线程
            proc.Kill();//结束FFmpeg进程
            proc.Dispose();//清理FFmpeg进程
            proc = null;
            Wnd_AllEnd();//窗体各控件重设
            listView1.Items[ActiveItem].SubItems[1].Text = "Ready";
            listView1.Items[ActiveItem].BackColor = Color.Red;//在列表中标记当前活动的项目为预备
        }
        private void listView1_DragDrop(object sender, DragEventArgs e)
        {/*列表拖拽松开事件*/
            String[] Files = (String[])e.Data.GetData(DataFormats.FileDrop);//Files数组存放所有拖拽入的文件路径
            for (int i = 0; i < Files.Length; i++)
            {//遍历Files路径并加入listView
                if (System.IO.Path.GetExtension(Files[i]) == ".mp4")//只加入后缀名为.mp4的文件
                {
                    listView1.Items.Add(new ListViewItem(new string[] { Files[i], "Ready" }));
                    listView1.Items[listView1.Items.Count-1].BackColor = Color.Red;
                }
            }
        }
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {/*列表拖拽到边界事件*/
            if (button1.Enabled == true && e.Data.GetData(DataFormats.FileDrop) != null)
            {//当且仅当准备状态才可向列表中拖拽文件，否则无效
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {/*列表项目双击事件*/
            int index = listView1.Items.IndexOf(listView1.FocusedItem);//获取双击项的下标值
            if (button1.Enabled == true && index != -1)//当且仅当准备状态才可双击删除，否则无效
                listView1.Items.RemoveAt(index);
        }
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {/*trackBar值变动事件*/
            if (proc != null && proc.HasExited == false)
            {//当且仅当进程存在并且进程并未退出的情况下设置优先级
                SetPriorityByTrackBar();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {/*窗体关闭前事件*/
            if (button1.Enabled == false)//若当前状态为转换或暂停则拒绝关闭
            {
                MessageBox.Show(null,"关闭程序前请停止当前任务！","有任务正在执行");
                e.Cancel = true;//取消关闭
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {/*主窗体加载事件*/
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            FBD.Description = "使用前请先选择输出文件夹，否则程序自动退出！\n请确保空间足够大！";
            if (DialogResult.OK == FBD.ShowDialog())
            {//确认选择
                OutputFolder = FBD.SelectedPath;//保存输出文件夹
            }
            else
            {//未选择
                MessageBox.Show(null, "用户未指定输出位置，自动退出！", "输出文件夹无效");
                this.Close();
                this.Dispose();
            }
        }
        private void label7_Click(object sender, EventArgs e)
        {/*优先级帮助按钮单击事件*/
            MessageBox.Show(null,"一般情况下，高级别优先级不能使转码过程加速。\n当其它程序占用率较高时，可以调高优先级以保证转码优先，或是调低优先级为其它任务让出时间。","关于优先级设置");
        }
    }
}
