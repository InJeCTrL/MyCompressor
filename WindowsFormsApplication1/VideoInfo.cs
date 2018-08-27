using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyCompressor
{
    /// <summary> 视频信息类
    /// </summary>
    public class VideoInfo
    {
        /// <summary> 视频总数，初始为0
        /// </summary>
        public static int VideoNum = 0;
        /// <summary> 视频输出文件夹路径
        /// </summary>
        public static String OutputFolder;
        /// <summary> 文件路径
        /// </summary>
        List<String> FilePath = new List<string>();
        /// <summary> 文件名
        /// </summary>
        List<String> FileName = new List<string>();
        /// <summary> 一部分参数，视频路径、编码格式、分辨率、平均码率(可扩展时间限制)
        /// </summary>
        List<String> Arguments_part1 = new List<string>();
        /// <summary> 二部分参数，输出文件名
        /// </summary>
        List<String> Arguments_part2 = new List<string>();
        /// <summary> 时间区间参数，若视频需剪辑，拼接到一部分参数前
        /// </summary>
        List<String> TimeLimit = new List<string>();
        /// <summary> 标识是否剪辑
        /// </summary>
        List<Boolean> EditFlag = new List<Boolean>();
        /// <summary> 若视频需剪辑，存放开始时间(Double)
        /// </summary>
        List<Double> tBegin = new List<Double>();
        /// <summary> 若视频需剪辑，存放停止时间(Double)
        /// </summary>
        List<Double> tEnd = new List<Double>();
        /// <summary> 添加新视频信息，并同步到listView
        /// </summary>
        /// <param name="FP">输入文件路径</param>
        public void AddVideo(String FP)
        {
            FilePath.Add(FP);//新增文件路径
            FileName.Add(Path.GetFileName(FP));//新增文件名
            Arguments_part1.Add("-i \"" + FP + "\" " + //文件名
                                "-vcodec libx264 " + //编码格式
                                "-s 480*360 " + //分辨率
                                "-b:v 384k \"");//码率
            Arguments_part2.Add("\\" + FileName.ElementAt(VideoNum) + "\"");//输出文件名
            TimeLimit.Add(null);//时间区间参数默认空
            EditFlag.Add(false);//默认标记不进行剪辑
            tBegin.Add(0.0);//默认0
            tEnd.Add(0.0);//默认0
            Form1.frm1.listView1.Items.Add(new ListViewItem(new string[] { FileName.ElementAt(VideoNum), "转码", "Ready", "0.0 %" }));//listView添加新项
            Form1.frm1.listView1.Items[VideoNum].BackColor = Color.Red;
            VideoNum++;
        }
        /// <summary> 删除视频信息，并同步到listView
        /// </summary>
        /// <param name="index">视频索引值</param>
        public void DeleteVideo(int index)
        {
            FilePath.RemoveAt(index);
            FileName.RemoveAt(index);
            Arguments_part1.RemoveAt(index);
            Arguments_part2.RemoveAt(index);
            TimeLimit.RemoveAt(index);
            EditFlag.RemoveAt(index);
            tBegin.RemoveAt(index);
            tEnd.RemoveAt(index);
            Form1.frm1.listView1.Items.RemoveAt(index);
            VideoNum--;
        }
        /// <summary>获取(时间区间参数+)第一部分参数实现限制用于拼接
        /// </summary>
        /// <param name="index">视频索引值</param>
        /// <returns></returns>
        public String GetAPartArg(int index)
        {
            if (EditFlag.ElementAt(index) == true)//视频有经过剪辑，需拼接区间参数与一部分参数
                return TimeLimit.ElementAt(index) + Arguments_part1.ElementAt(index);
            else//视频未经剪辑
                return Arguments_part1.ElementAt(index);
        }
        /// <summary> 获取最后部分参数用于拼接
        /// </summary>
        /// <param name="index">视频索引值</param>
        /// <returns></returns>
        public String GetBPartArg(int index)
        {
            return Arguments_part2.ElementAt(index);
        }
        /// <summary> 获取文件路径
        /// </summary>
        /// <param name="index">视频索引值</param>
        /// <returns></returns>
        public String GetFilePath(int index)
        {
            return FilePath.ElementAt(index);
        }
        /// <summary> 返回文件名
        /// </summary>
        /// <param name="index">视频索引值</param>
        /// <returns></returns>
        public String GetFileName(int index)
        {
            return FileName.ElementAt(index);
        }
        /// <summary> 获取剪辑后的视频起始时间(String)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频起始时间(String)</returns>
        public String GetBeginTime_str(int index)
        {
            DateTime tTime = new DateTime(1970, 1, 1);
            tTime = tTime.AddSeconds((int)tBegin.ElementAt(index));
            return tTime.ToString("HH:mm:ss");
        }
        /// <summary> 获取剪辑后的视频结束时间(String)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频结束时间(String)</returns>
        public String GetEndTime_str(int index)
        {
            DateTime tTime = new DateTime(1970, 1, 1);
            tTime = tTime.AddSeconds((int)tEnd.ElementAt(index));
            return tTime.ToString("HH:mm:ss");
        }
        /// <summary> 获取剪辑后的视频起始时间(Double)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频起始时间(Double)</returns>
        public Double GetBeginTime_double(int index)
        {
            return tBegin.ElementAt(index);
        }
        /// <summary> 获取剪辑后的视频结束时间(Double)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频结束时间(Double)</returns>
        public Double GetEndTime_double(int index)
        {
            return tEnd.ElementAt(index);
        }
        /// <summary> 获取剪辑后的视频开始时间(int)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频开始时间(int)</returns>
        public int GetBeginTime_int(int index)
        {
            return (int)tBegin.ElementAt(index);
        }
        /// <summary> 获取剪辑后的视频结束时间(int)
        /// </summary>
        /// <param name="index"></param>
        /// <returns>剪辑后的视频结束时间(int)</returns>
        public int GetEndTime_int(int index)
        {
            return (int)tEnd.ElementAt(index);
        }
        /// <summary> 标记本条视频剪辑时长，并同步到listView
        /// </summary>
        /// <param name="_tBegin">剪辑起点</param>
        /// <param name="_tEnd">剪辑终点</param>
        /// <param name="index">视频索引值</param>
        public void AddTimeLimit(Double _tBegin, Double _tEnd, int index)
        {
            tBegin[index] = _tBegin;
            tEnd[index] = _tEnd;//修改剪辑起止时间
            EditFlag[index] = true;//确认剪辑
            String str_tBegin = GetBeginTime_str(index);//浮点时间转字符串
            String str_tEnd = GetEndTime_str(index);//浮点时间转字符串
            String TimeLimit_str = "-ss " + str_tBegin + " -to " + str_tEnd + " ";
            TimeLimit[index] = TimeLimit_str;//修改时间区间
            Form1.frm1.listView1.Items[index].SubItems[1].Text = "转码&剪辑";
        }
        /// <summary> 标记视频无需剪辑，并同步到listView
        /// </summary>
        /// <param name="index">视频索引值</param>
        public void DisableTimeLimit(int index)
        {
            EditFlag[index] = false;//视频剪辑标志失效
            Form1.frm1.listView1.Items[index].SubItems[1].Text = "转码";//更新listView标记
        }
        /// <summary> 获取视频是否经过剪辑
        /// </summary>
        /// <param name="index">视频索引值</param>
        /// <returns></returns>
        public Boolean GetEditFlag(int index)
        {
            return EditFlag.ElementAt(index);
        }
        /// <summary> 清空视频记录，并同步到listView
        /// </summary>
        public void Clear()
        {
            FilePath.Clear();
            FileName.Clear();
            Arguments_part1.Clear();
            Arguments_part2.Clear();
            EditFlag.Clear();
            tBegin.Clear();
            tEnd.Clear();
            Form1.frm1.listView1.Items.Clear();
            VideoNum = 0;
        }
    };
}
