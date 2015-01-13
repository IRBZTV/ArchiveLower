using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using ArchiveLower.MyDBTableAdapters;
using System.Diagnostics;

namespace ArchiveLower
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        protected void FindDuration(string Str)
        {

            try
            {
                string TimeCode = "";
                if (Str.Contains("Duration:"))
                {
                    TimeCode = Str.Substring(Str.IndexOf("Duration: "), 21).Replace("Duration: ", "").Trim();
                    string[] Times = TimeCode.Split('.')[0].Split(':');
                    double Frames = double.Parse(Times[0].ToString()) * (3600) * (25) +
                        double.Parse(Times[1].ToString()) * (60) * (25) +
                        double.Parse(Times[2].ToString()) * (25);
                    progressBar1.Maximum = int.Parse(Frames.ToString());
                    //label2.Text = Frames.ToString();

                }
                if (Str.Contains("time="))
                {
                    try
                    {
                        string CurTime = "";
                        CurTime = Str.Substring(Str.IndexOf("time="), 16).Replace("time=", "").Trim();
                        string[] CTimes = CurTime.Split('.')[0].Split(':');
                        double CurFrame = double.Parse(CTimes[0].ToString()) * (3600) * (25) +
                            double.Parse(CTimes[1].ToString()) * (60) * (25) +
                            double.Parse(CTimes[2].ToString()) * (25);
                        progressBar1.Value = int.Parse(CurFrame.ToString());

                        label1.Text = ((progressBar1.Value * 100) / progressBar1.Maximum).ToString() + "%";
                        // label3.Text = CurFrame.ToString();
                        Application.DoEvents();
                    }
                    catch
                    {


                    }

                }
                if (Str.Contains("fps="))
                {

                    string Speed = "";

                    Speed = Str.Substring(Str.IndexOf("fps="), 8).Replace("fps=", "").Trim();

                    label4.Text = "Speed: " + (float.Parse(Speed) / 25).ToString() + " X ";
                    Application.DoEvents();
                }
            }
            catch (Exception exp)
            {

                richTextBox2.Text += exp.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            MASTER_DATATableAdapter Ta = new MASTER_DATATableAdapter();
            XmlDocument XDoc = new XmlDocument();
            string XmlPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\list.xml";
            if (System.IO.File.Exists(XmlPath))
            {
                XDoc.Load(XmlPath);
                //Load programs from xml config
                XmlNodeList PrXmlLst = XDoc.GetElementsByTagName("Program");
                foreach (XmlNode Nd in PrXmlLst)
                {
                    decimal Month = decimal.Parse(Nd.Attributes["Months"].Value.ToString());
                    int ProgId = int.Parse(Nd.Attributes["Id"].Value.ToString());
                    string Bitrate = Nd.Attributes["Bitrate"].Value.ToString();
                    //Load Data from DB where  not lowered:
                    MyDB.MASTER_DATADataTable Dt = Ta.LoadData(Month, ProgId);
                    for (int i = 0; i < Dt.Rows.Count; i++)
                    {
                        //Convert to Bitrate:
                        string inFile = Dt.Rows[0]["VIDEO_PATH_HI"].ToString();
                        string outFile = Path.GetDirectoryName(Application.ExecutablePath) +"\\"+ Dt.Rows[0]["Id"].ToString()+".mp4";
                        if (File.Exists(inFile))
                        {
                            VideoConverter(inFile, outFile, Bitrate);

                            //Copy to server
                            File.Copy(outFile, inFile, true);
                            File.Delete(outFile);
                            //Update to islowered=1
                            Ta.Update_Islowered(int.Parse(Dt.Rows[0]["Id"].ToString()));
                        }
                        
                    }
                }
            }
            timer1.Enabled = true;
        }
        public void VideoConverter(string InFileName,string OutFileName,string Bitrate)
        {

            richTextBox2.Text += "=============\n";
            richTextBox2.Text += DateTime.Now.ToString() + ":\n" + "Start Convert:\n" + InFileName + " \n";
            richTextBox2.Text += "=============\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();
            //string OutDir = ConfigurationSettings.AppSettings["OutPutDirectory"].Trim();
            //if (!Directory.Exists(OutDir))
            //{
            //    Directory.CreateDirectory(OutDir);
            //}
            Process proc = new Process();
            if (Environment.Is64BitOperatingSystem)
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg64";
            }
            else
            {
                proc.StartInfo.FileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\ffmpeg32";
            }

            proc.StartInfo.Arguments = "-i " + "\"" + InFileName + "\"" + "  -b "+Bitrate+"K -y " + "\"" + OutFileName+ "\"";
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = true;
           // proc.Exited += new EventHandler(VideoConvert_Exited);

            if (!proc.Start())
            {
                richTextBox1.Text += " \n" + "Error starting";
                return;
            }
            proc.PriorityClass = ProcessPriorityClass.RealTime;


            StreamReader reader = proc.StandardError;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (richTextBox1.Lines.Length > 6)
                {
                    richTextBox1.Text = "";
                }
                FindDuration(line);
                richTextBox1.Text += (line) + " \n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                Application.DoEvents();
            }
            progressBar1.Value = progressBar1.Maximum;
            label1.Text = "100%";
            proc.Close();
            richTextBox2.Text += "=============\n";
            richTextBox2.Text += DateTime.Now.ToString() + ":\n" + "End Convert:\n" + InFileName + " \n";
            richTextBox2.Text += "=============\n";
            richTextBox2.SelectionStart = richTextBox2.Text.Length;
            richTextBox2.ScrollToCaret();
            Application.DoEvents();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button2.Text = "Started";
            button2.BackColor = Color.Red;

            button2_Click(null, null);

            button2.Text = "Start";
            button2.BackColor = Color.Navy;
        }
    }
}
