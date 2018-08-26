using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyCompressor
{
    public partial class Form2 : Form
    {
        double tBegin, tEnd;//截取的起止时间
        public Form2()
        {
            InitializeComponent();
            axWindowsMediaPlayer1.URL = Form1.frm1.VideoInf.GetFilePath(Form1.frm1.listView1.FocusedItem.Index);//加载选中视频
        }
        /// <summary> 检查各截取时间是否合法
        /// </summary>
        /// <returns>返回检测值 0：正确 1：终止点小等于起始点 2：起始点文本错误 3：终止点文本错误 4：终止点超出视频长度</returns>
        private int CheckTimeLimit()
        {
            DateTime tDatetime;
            if (DateTime.TryParseExact(textBox1.Text, "HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture,
                                       System.Globalization.DateTimeStyles.AdjustToUniversal,
                                       out tDatetime))//起始点文本判定
            {
                tBegin = tDatetime.Hour * 3600 +
                         tDatetime.Minute * 60 +
                         tDatetime.Second;//更新起始点
                if (DateTime.TryParseExact(textBox2.Text, "HH:mm:ss",
                                           System.Globalization.CultureInfo.InvariantCulture,
                                           System.Globalization.DateTimeStyles.AdjustToUniversal,
                                           out tDatetime))//终止点文本判定
                {
                    tEnd = tDatetime.Hour * 3600 +
                           tDatetime.Minute * 60 +
                           tDatetime.Second;//更新终止点
                    if (tEnd > axWindowsMediaPlayer1.currentMedia.duration)
                        return 4;
                    if (tEnd <= tBegin)//终止点小等于起始点
                        return 1;
                    return 0;
                }
                else
                {
                    return 3;
                }
            }
            else
                return 2;
        }
        /// <summary> 截取左起始点，在textBox1中显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            tBegin = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;//获取左起始点
            DateTime tTime = new DateTime(1970,1,1);
            tTime = tTime.AddSeconds((int)tBegin);
            textBox1.Text = tTime.ToString("HH:mm:ss");//并显示
        }
        /// <summary> 截取右终止点，在textBox2中显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            tEnd = axWindowsMediaPlayer1.Ctlcontrols.currentPosition;//获取右终止点
            int ChkRet = CheckTimeLimit();//检查时间限制
            switch (ChkRet)
            {
                case 1:
                    MessageBox.Show(null, "右终止点时间需大于左起始点\n请重新截取！", "截取错误");
                    break;
                case 2:
                    MessageBox.Show(null, "左起始点格式有误\n请重新截取！", "格式错误");
                    break;
                default:
                    DateTime tTime = new DateTime(1970,1,1);
                    tTime = tTime.AddSeconds((int)tEnd);
                    textBox2.Text = tTime.ToString("HH:mm:ss");
                    break;
            }
        }
        /// <summary> 完成视频剪辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            int ChkRet = CheckTimeLimit();//检查时间限制
            switch (ChkRet)
            {
                case 1:
                    MessageBox.Show(null, "右终止点时间需大于左起始点\n请重新截取！", "截取错误");
                    break;
                case 2:
                    MessageBox.Show(null, "左起始点格式有误\n请重新截取！", "格式错误");
                    break;
                case 3:
                    MessageBox.Show(null, "右终止点格式有误\n请重新截取！", "格式错误");
                    break;
                case 4:
                    MessageBox.Show(null, "右终止点超出视频长度\n请重新截取！", "截取错误");
                    break;
                default:
                    Form1.frm1.VideoInf.AddTimeLimit(tBegin,tEnd,
                                            Form1.frm1.listView1.FocusedItem.Index);
                    this.Close();
                    break;
            }
        }
        /// <summary> 取消视频剪辑
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
