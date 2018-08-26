using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyCompressor
{
    /// <summary> 视频转码类
    /// </summary>
    class TransCode
    {
        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess([In] IntPtr processHandle);
        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess([In] IntPtr processHandle);

        /// <summary> 保存各视频信息供调用
        /// </summary>
        VideoInfo vInf;
        /// <summary> 新建的FFmpeg进程
        /// </summary>
        Process proc;
        /// <summary> 转码队列线程
        /// </summary>
        Thread TaskQueue;
        /// <summary> 存储当前正在转换的视频时长
        /// </summary>
        int tDuration;
        /// <summary> 标记当前正在处理的项目下标
        /// </summary>
        public static int ActiveItemIndex;
        /// <summary> 保存优先级数值，初始为1
        /// </summary>
        int PriorityVal = 1;
        /// <summary> 设置视频转码类的视频信息对象
        /// </summary>
        /// <param name="_vInf">视频信息</param>
        public void SetVideoInfo(VideoInfo _vInf)
        {
            vInf = _vInf;
        }
        /// <summary> 设置FFmpeg进程优先级
        /// </summary>
        /// <param name="Val">优先级 0：低 1：标准 2：高</param>
        public void SetPriority(int Val)
        {
            PriorityVal = Val;//设置保存的优先级数值
            switch (Val)
            {/*根据Val设置优先级信息*/
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
        /// <summary> 获得视频时长
        /// </summary>
        /// <param name="Line">FFmpeg输出行</param>
        /// <returns>秒数</returns>
        public int GetDuration(string Line)
        {
            if (vInf.GetEditFlag(ActiveItemIndex))
            {/*视频经过剪辑*/
                return vInf.GetEndTime_int(ActiveItemIndex) - vInf.GetBeginTime_int(ActiveItemIndex);
            }
            else
            {/*视频未经剪辑*/
                string regStr = @"Duration: (\d+):(\d+):(\d+)";//正则匹配Duration: 00:00:00
                Match MatchResult = Regex.Match(Line, regStr);
                return int.Parse(MatchResult.Groups[1].Value) * 3600 +
                       int.Parse(MatchResult.Groups[2].Value) * 60 +
                       int.Parse(MatchResult.Groups[3].Value);
            }
        }
        /// <summary> 获得当前转换进度
        /// </summary>
        /// <param name="Line">FFmpeg输出行</param>
        /// <returns>秒数</returns>
        public int GetNowStatus(string Line)
        {
            string regStr = @"time=(\d+):(\d+):(\d+)";//正则匹配time=00:00:00
            Match MatchResult = Regex.Match(Line, regStr);
            int NowStatus = int.Parse(MatchResult.Groups[1].Value) * 3600 +
                            int.Parse(MatchResult.Groups[2].Value) * 60 +
                            int.Parse(MatchResult.Groups[3].Value);
            return NowStatus;
        }
        /// <summary> 处理重定向的FFmpeg输出行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">FFmpeg输出行</param>
        private void FFmpeg_Output(object sender, DataReceivedEventArgs e)
        {
            if (e != null && e.Data != null)
            {
                double tVal;//记录临时计算的进度
                if (e.Data.Contains("time="))//若某行包含time=字样代表该行为转换输出行
                {
                    tVal = GetNowStatus(e.Data) * 100.0 / tDuration;//计算百分比
                    Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[3].Text = tVal.ToString("f1") + " %";//列表中状态列显示百分比
                    Form1.frm1.progressBar1.Value = (int)tVal;//计算转换进度并更新进度条
                }
                else if (e.Data.Contains("Duration"))//若某行包含Duration字样代表该行记录时长
                    tDuration = GetDuration(e.Data);
            }
        }
        /// <summary> 创建FFmpeg并执行任务队列
        /// </summary>
        private void TransCodeProc()
        {
            for (ActiveItemIndex = 0; ActiveItemIndex < VideoInfo.VideoNum; ActiveItemIndex++)
            {//列表从上至下进行压缩
                Form1.frm1.Text = ActiveItemIndex + " / " + VideoInfo.VideoNum;//设置标题栏标记总进度
                if (File.Exists(VideoInfo.OutputFolder + "/" + vInf.GetFileName(ActiveItemIndex)))//目标文件已存在
                {//检测输出文件夹下是否已存在同名文件，若存在则询问覆盖或跳过
                    if (DialogResult.No == MessageBox.Show(null, "文件已经存在于输出文件夹中\n是：覆盖生成，否：跳过此文件", "文件重复", MessageBoxButtons.YesNo))
                    {//用户选择跳过，标记为Complete并执行下一条压缩
                        Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[3].Text = "0:0:0";
                        Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Complete";
                        Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.LawnGreen;
                        continue;
                    }
                    else//用户选择覆盖，删除原有同名文件
                        File.Delete(VideoInfo.OutputFolder + "/" + vInf.GetFileName(ActiveItemIndex));
                }
                Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Compressing";//标记转换中
                Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.Gold;
                proc = new Process();//初始化进程
                proc.StartInfo.FileName = "ffmpeg.exe";//目标为同目录下的ffmpeg
                proc.StartInfo.UseShellExecute = false;//不使用shell启动
                proc.StartInfo.CreateNoWindow = true;//静默无窗体显示
                proc.StartInfo.RedirectStandardError = true;//重定向标准错误输出流
                proc.StartInfo.Arguments = vInf.GetAPartArg(ActiveItemIndex) + //一部分参数
                                           VideoInfo.OutputFolder + //输出文件夹参数
                                           vInf.GetBPartArg(ActiveItemIndex);//二部分参数
                proc.ErrorDataReceived += new DataReceivedEventHandler(FFmpeg_Output);//输出流附加到FFmpeg_Output上
                proc.Start();//启动ffmpeg进程，开始转换
                DateTime BeginTime = DateTime.Now;//记录开始时间
                SetPriority(PriorityVal);//设置FFmpeg进程优先值
                Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.Gold;//并标记本条颜色为金黄色，表明处于压缩状态
                proc.BeginErrorReadLine();//开始读取错误输出流
                proc.WaitForExit();//等待ffmpeg进程退出
                TimeSpan TransTime = DateTime.Now - BeginTime;//计算时间长
                Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Complete";
                Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[3].Text = TransTime.Hours.ToString() + ":" +
                                                                               TransTime.Minutes.ToString() + ":" +
                                                                               TransTime.Seconds.ToString();//标记本条转换状态为结束
                Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.LawnGreen;
            }
            if (proc != null)//当且仅当进程被创建过才关闭并清理进程，避免异常
            {
                proc.Close();//关闭进程
                proc.Dispose();//清理进程
                proc = null;
            }
            ActiveItemIndex = 0;//当前转换下标归零
            Form1.frm1.button1.Enabled = true;//转换正常结束，开始按键有效
        }
        /// <summary> 开始转码
        /// </summary>
        public void Begin()
        {
            Form1.frm1.button1.Enabled = false;//开始按钮无效
            TaskQueue = new Thread(TransCodeProc);
            TaskQueue.IsBackground = true;//创建后台转码队列线程
            TaskQueue.Priority = ThreadPriority.Highest;//转码队列线程最高优先级
            TaskQueue.Start();//启动线程
        }
        /// <summary> 暂停转码
        /// </summary>
        public void Pause()
        {
            NtSuspendProcess(proc.Handle);//挂起FFmpeg进程
            Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Pause";//标记状态为暂停
            Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.Red;
        }
        /// <summary> 恢复转码
        /// </summary>
        public void Resume()
        {
            NtResumeProcess(proc.Handle);//恢复FFmpeg进程
            Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Compressing";//标记状态为转换
            Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.Gold;
        }
        /// <summary> 停止转码
        /// </summary>
        public void Stop()
        {
            TaskQueue.Abort();//终止任务队列线程
            proc.Kill();//结束FFmpeg进程
            proc.Dispose();//清理FFmpeg进程
            Form1.frm1.button1.Enabled = true;////开始按钮有效
            Form1.frm1.listView1.Items[ActiveItemIndex].SubItems[2].Text = "Ready";
            Form1.frm1.listView1.Items[ActiveItemIndex].BackColor = Color.Red;//在列表中标记当前活动的项目为预备
        }
    }
}
