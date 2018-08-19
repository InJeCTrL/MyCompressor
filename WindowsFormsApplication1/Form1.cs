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
            for (int i = 0; i < listView1.Items.Count; i++)
            {
                if (File.Exists("E:\\A\\" + System.IO.Path.GetFileName(listView1.Items[i].SubItems[0].Text)))//目标文件已存在
                {
                    if (DialogResult.No == MessageBox.Show(null, "文件已经存在于输出文件夹中\n是：覆盖生成，否：跳过此文件", "文件重复", MessageBoxButtons.YesNo))
                    {
                        listView1.Items[i].SubItems[1].Text = "Complete";
                        listView1.Items[i].BackColor = Color.LawnGreen;
                        continue;
                    }
                    else
                        File.Delete("E:\\A\\" + System.IO.Path.GetFileName(listView1.Items[i].SubItems[0].Text));
                }
                procinfo.Arguments = "-i " + listView1.Items[i].SubItems[0].Text + " -vcodec h264 -s 480*360 -b:v 384k " + "E:\\A\\" + System.IO.Path.GetFileName(listView1.Items[i].SubItems[0].Text);
                proc = Process.Start(procinfo);
                listView1.Items[i].SubItems[1].Text = "Compressing";
                listView1.Items[i].BackColor = Color.Gold;
                proc.WaitForExit();
                proc.Close();
                listView1.Items[i].SubItems[1].Text = "Complete";
                listView1.Items[i].BackColor = Color.LawnGreen;
            }
            listView1.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = false;
            button2.Text = "暂停";
            button3.Enabled = false;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
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
