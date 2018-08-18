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
namespace MyCompressor
{
    public partial class Form1 : Form
    {
        [DllImport("ntdll.dll")]
        private static extern int NtResumeProcess([In] IntPtr processHandle);
        [DllImport("ntdll.dll")]
        private static extern int NtSuspendProcess([In] IntPtr processHandle);

        Process proc;
        Thread th;
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }
        private void ComPressor()
        {
            ProcessStartInfo procinfo = new ProcessStartInfo("ffmpeg.exe");
            procinfo.UseShellExecute = false;
            procinfo.CreateNoWindow = true;
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (File.Exists("E:\\A\\" + System.IO.Path.GetFileName((String)listBox1.Items[i])))//目标文件已存在
                {
                    if (DialogResult.No == MessageBox.Show(null, "文件已经存在于输出文件夹中\n是：覆盖生成，否：跳过此文件", "文件重复", MessageBoxButtons.YesNo))
                    {
                        listBox2.Items[i] = "Complete";
                        continue;
                    }
                    else
                        File.Delete("E:\\A\\" + System.IO.Path.GetFileName((String)listBox1.Items[i]));
                }
                procinfo.Arguments = "-i " + (String)listBox1.Items[i] + " -vcodec h264 -s 480*360 -b:v 384k " + "E:\\A\\" + System.IO.Path.GetFileName((String)listBox1.Items[i]);
                proc = Process.Start(procinfo);
                listBox2.Items[i] = "Compressing";
                proc.WaitForExit();
                proc.Close();
                listBox2.Items[i] = "Complete";
            }
            listBox1.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = false;
            button2.Text = "暂停";
            button3.Enabled = false;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = true;
            button2.Text = "暂停";
            button3.Enabled = true;
            th = new Thread(ComPressor);
            th.IsBackground = true;
            th.Start();
        }
        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            String[] Files = (String[])e.Data.GetData(DataFormats.FileDrop);
            int i;
            int count = listBox1.Items.Count + 1;
            for (i = 0; i < Files.Length; i++)
            {
                if (System.IO.Path.GetExtension(Files[i]) == ".mp4")
                {
                    listBox1.Items.Add(Files[i]);
                    listBox2.Items.Add("Ready");
                    
                }
            }
        }
        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBox1.SelectedIndex;
            if (index != -1)
            {
                listBox1.Items.RemoveAt(index);
                listBox2.Items.RemoveAt(index);
            }
        }
        private void listBox2_DrawItem(object sender, DrawItemEventArgs e)
        {
            Brush setcolor = Brushes.Black;
            if (e.Index >= 0)
            {
                string txt = listBox2.Items[e.Index].ToString();
                if (txt.Equals("Ready"))
                    e.Graphics.FillRectangle(Brushes.Red, e.Bounds);
                else if (txt.Equals("Compressing"))
                    e.Graphics.FillRectangle(Brushes.Yellow, e.Bounds);
                else
                    e.Graphics.FillRectangle(Brushes.Green, e.Bounds);
                e.Graphics.DrawString(txt, e.Font, setcolor, e.Bounds, StringFormat.GenericDefault);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text.Equals("暂停"))
            {
                NtSuspendProcess(proc.Handle);
                button2.Text = "继续";
            }
            else
            {
                NtResumeProcess(proc.Handle);
                button2.Text = "暂停";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            th.Abort();
            proc.Kill();
            button1.Enabled = true;
            button2.Enabled = false;
            button2.Text = "暂停";
            button3.Enabled = false;
            listBox1.Enabled = true;
            for (int i = 0; i < listBox2.Items.Count; i++)
                if (listBox2.Items[i].ToString().Equals("Compressing"))
                    listBox2.Items[i] = "Ready";
        }
    }
}
