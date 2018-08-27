using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
namespace MyCompressor
{
    public partial class Form1 : Form
    {
        public static Form1 frm1;

        public VideoInfo VideoInf = new VideoInfo();//实例化视频信息类
        TransCode TCode = new TransCode();//初始化视频转码类

        /// <summary> Form初始化
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            frm1 = this;
            CheckForIllegalCrossThreadCalls = false;//取消线程安全
        }
        /// <summary> 优先级trackBar变动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            TCode.SetPriority(trackBar1.Value);
        }
        /// <summary> 列表项目右键弹出菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right && //列表项右键时触发
                button1.Enabled == true && //当且仅当准备状态有效
                listView1.SelectedItems.Count == 1)//选定了一个列表项
            {
                if (VideoInf.GetEditFlag(listView1.FocusedItem.Index))//选中条经过剪辑，可取消剪辑
                    取消剪辑ToolStripMenuItem.Enabled = true;
                else//选中条未经编辑，不可取消剪辑
                    取消剪辑ToolStripMenuItem.Enabled = false;
                contextMenuStrip1.Show(listView1, e.Location);//相对于列表弹出右键菜单
            }
        }
        /// <summary> 显示优先级帮助
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label7_Click(object sender, EventArgs e)
        {
            MessageBox.Show(null, "一般情况下，高级别优先级不能使转码过程加速。\n当其它程序占用率较高时，可以调高优先级以保证转码优先，或是调低优先级为其它任务让出时间。", "关于优先级设置");
        }
        /// <summary> 拖拽到列表边界事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (button1.Enabled == true && e.Data.GetData(DataFormats.FileDrop) != null)
            {//当且仅当准备状态才可向列表中拖拽文件，否则无效
                e.Effect = DragDropEffects.Copy;
            }
        }
        /// <summary> 列表拖拽松开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            String[] Files = (String[])e.Data.GetData(DataFormats.FileDrop);//Files数组存放所有拖拽入的文件路径
            for (int i = 0; i < Files.Length; i++)
            {//遍历Files路径并加入listView
                if (Path.GetExtension(Files[i]) == ".mp4")//只加入后缀名为.mp4的文件
                {
                    VideoInf.AddVideo(Files[i]);//新增一条视频记录
                }
            }
        }
        /// <summary> 设置输出文件夹，成功返回0，失败返回1
        /// </summary>
        private int SetOutputFolder()
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            FBD.Description = "使用前请先选择输出文件夹，否则程序自动退出！\n请确保空间足够大！";
            if (DialogResult.OK == FBD.ShowDialog())
            {//确认选择
                VideoInfo.OutputFolder = FBD.SelectedPath;//保存输出文件夹
                textBox1.Text = VideoInfo.OutputFolder;//显示输出文件夹路径
                return 0;
            }
            else
            {//未选择
                return 1;
            }
        }
        /// <summary> 单击以重设输出文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            SetOutputFolder();
        }
        /// <summary> 检查本程序目录下是否存在剪辑必须文件
        /// </summary>
        /// <returns>返回检查值 0：文件完整 1：只缺少AxInterop 2：只缺少Interop</returns>
        private int CheckEditFileExists()
        {
            int ret = 0;
            if (!File.Exists(Environment.CurrentDirectory + "/AxInterop.WMPLib.dll"))//AxInterop不存在
                ret++;
            if (!File.Exists(Environment.CurrentDirectory + "/Interop.WMPLib.dll"))//Interop不存在
                ret += 2;
            return ret;
        }
        /// <summary> 检查本程序目录下是否存在FFmpeg
        /// </summary>
        /// <returns></returns>
        private Boolean CheckFFmpegExists()
        {
            if (!File.Exists(Environment.CurrentDirectory + "/ffmpeg.exe"))//FFmpeg不存在
                return false;
            return true;
        }
        /// <summary> 主窗体加载时检测必须文件是否存在，保存输出文件夹路径，并初始化视频转码类的视频信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            if (!CheckFFmpegExists())
            {//检查无FFmpeg存在
                MessageBox.Show(null, "缺少FFmpeg！\n若您更新过FFmpeg，请将FFmpeg主程序重命名为\"ffmpeg.exe\"与本程序置于同一目录下。", "文件缺失");
                Application.Exit();
            }
            if (SetOutputFolder() == 1)
            {
                MessageBox.Show(null, "用户未指定输出位置，自动退出！", "输出文件夹无效");
                Application.Exit();
            }
            TCode.SetVideoInfo(VideoInf);//启动时设置视频转码类中视频信息
        }
        /// <summary> 开始转码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            TCode.Begin();
        }
        /// <summary> 暂停转码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("暂停"))
            {//若当前是转换状态
                TCode.Pause();
                button2.Text = "继续";//更新按钮标记
            }
            else
            {//若当前是暂停状态
                TCode.Resume();
                button2.Text = "暂停";//更新按钮标记
            }
        }
        /// <summary> 停止转码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            TCode.Stop();
        }
        /// <summary> 窗口关闭时判断是否有任务正在进行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (button1.Enabled == false)//若当前状态为转换或暂停则拒绝关闭
            {
                MessageBox.Show(null, "关闭程序前请停止当前任务！", "有任务正在执行");
                e.Cancel = true;//取消关闭
            }
        }
        /// <summary> 开始停止切换时界面变动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_EnabledChanged(object sender, EventArgs e)
        {
            if (button1.Enabled == true)
            {//任务停止后
                button1.Visible = true;//显示开始按钮
                progressBar1.Visible = false;//隐藏进度条
                button2.Enabled = false;//禁止暂停
                button2.Text = "暂停";
                button3.Enabled = false;//禁止停止
                button7.Enabled = true;//运行清空
                button8.Enabled = true;//允许重设输出目录
                this.Text = "MyCompressor";//恢复标题栏
            }
            else//任务开始后
            {
                button1.Visible = false;//隐藏开始按钮
                progressBar1.Visible = true;//显示进度条
                button2.Enabled = true;//允许暂停
                button3.Enabled = true;//允许停止
                button7.Enabled = false;//禁止清空
                button8.Enabled = false;//禁止重设输出目录
            }
        }
        /// <summary> 任务列表清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            VideoInf.Clear();
        }
        /// <summary> ListView右键删除操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VideoInf.DeleteVideo(listView1.FocusedItem.Index);//删除视频记录
        }
        /// <summary> ListView右键剪辑操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 剪辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (CheckEditFileExists())//判断MediaPlayer必须文件
            {
                case 1:
                    MessageBox.Show(null, "缺少AxInterop，您将无法使用剪辑功能！\n请将\"AxInterop.WMPLib.dll\"与本程序置于同一目录下。", "文件缺失");
                    break;
                case 2:
                    MessageBox.Show(null, "缺少Interop，您将无法使用剪辑功能！\n请将\"Interop.WMPLib.dll\"与本程序置于同一目录下。", "文件缺失");
                    break;
                case 3:
                    MessageBox.Show(null, "缺少AxInterop与Interop，您将无法使用剪辑功能！\n请将\"AxInterop.WMPLib.dll\"与\"Interop.WMPLib.dll\"与本程序置于同一目录下。", "文件缺失");
                    break;
                default:
                    Form2 frm2 = new Form2(listView1.FocusedItem.Index);//初始化剪辑窗口，并传入当前列表中选中项下标
                    frm2.ShowDialog();//模态启动剪辑窗口
                    break;
            }
        }
        /// <summary> ListView右键取消剪辑操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 取消剪辑ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            VideoInf.DisableTimeLimit(listView1.FocusedItem.Index);//取消剪辑操作
        }
    }
}
