using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExirDownloader
{
    public partial class frmDownloader : Form
    {
        public frmDownloader()
        {
            InitializeComponent();
        }

        const string NumericFormat = "###,###,###,###,###,###,###";
        //more than enough for uint64:
        //18,446,744,073,709,551,615 max

        static string CreateFormat(
          string preFormat, string placeholder)
        {
            return preFormat.Replace(placeholder, NumericFormat);
        } //CreateFormat

        static string ConvertUrlToFileName(string url)
        {
            string[] terms = url.Split(
                new string[] { ":", "//" },
                StringSplitOptions.RemoveEmptyEntries);
            string fname = terms[terms.Length - 1];
            fname = fname.Replace('/', '.');
            return fname;
        } //ConvertUrlToFileName

        static long GetExistingFileLength(string filename)
        {
            if (!File.Exists(filename)) return 0;
            FileInfo info = new FileInfo(filename);
            return info.Length;
        } //GetExistingFileLength

        private bool DownloadComplete = false;
        private bool DownloadCancel = false;
        private void DownloadOne(
          string url, string existingFilename, bool quiet)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            HttpWebRequest webRequest;
            HttpWebResponse webResponse;
            IWebProxy proxy = null; //SA???
            string fmt = CreateFormat(
                "{0}: {1:#} of {2:#} ({3:g3}%)", "#");
            FileStream fs = null;


            try
            {
                string fname = existingFilename;
                if (fname == null)
                    fname = ConvertUrlToFileName(url);
                webRequest = (HttpWebRequest)WebRequest.Create(url);
                long preloadedLength = GetExistingFileLength(fname);
                if (preloadedLength > 0)
                    webRequest.AddRange((int)preloadedLength);
                webRequest.Proxy = proxy; //SA??? or DefineProxy
                webResponse = (HttpWebResponse)webRequest.GetResponse();
                fs = new FileStream(
                    fname, FileMode.Append, FileAccess.Write);
                long fileLength = webResponse.ContentLength;
                string todoFormat = CreateFormat(
                    @"Downloading {0}: {1:#} bytes...", "#");

                ChangeStatus(string.Format(todoFormat, url, fileLength));
                //Console.WriteLine( "Pre-loaded: preloaded length {0}", preloadedLength);
                //Console.WriteLine("Remaining length: {0}", fileLength);
                Stream strm = webResponse.GetResponseStream();
                int arrSize = 10 * 1024 * 1024; //SA???                 
                byte[] barr = new byte[arrSize];
                long bytesCounter = preloadedLength;
                string fmtPercent = string.Empty;

                if (preloadedLength == webResponse.ContentLength)
                {
                    Completing();
                }

                while (true)
                {
                    if (DownloadCancel)
                        break;

                    int actualBytes = strm.Read(barr, 0, arrSize);
                    if (actualBytes <= 0)
                        break;
                    fs.Write(barr, 0, actualBytes);
                    bytesCounter += actualBytes;
                    double percent = 0d;
                    if (fileLength > 0)
                        percent =
                            100.0d * bytesCounter /
                            (preloadedLength + fileLength);
                    if (!quiet)
                        ChangeStatus(string.Format(
                             fmt,
                             fname,
                             bytesCounter,
                             preloadedLength + fileLength,
                             percent));
                } //loop

                if (!DownloadCancel)
                {
                    ChangeStatus(string.Format(@"{0}: complete!", url));
                    Completing();
                }
            }
            catch (Exception e)
            {
                ChangeStatus(
                    string.Format(
                     "{0}: {1} '{2}'",
                     url, e.GetType().FullName,
                     e.Message));
                if (e.Message.Contains("416"))
                {
                    Completing();
                }
            }
            finally
            {
                if (fs != null)
                {
                    fs.Flush();
                    fs.Close();
                } //if
            } //exception
        } //DownloadOne



        public void ChangeStatus(string s)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke((Action)delegate
                {
                    lblStatus.Text = s;
                });
            }
            else
            {
                lblStatus.Text = s;
            }
            Application.DoEvents();
        }




        string url = "", ver = "";
        string folderToSave = "";
        string fileToSave = "";

        private void frmDownloader_FormClosing(object sender, FormClosingEventArgs e)
        {
            DownloadCancel = true;
        }

        private void Completing()
        {
            DownloadComplete = true;
            btnUpgrade.Invoke((Action)delegate
            {
                btnUpgrade.Text = "Open";
                btnUpgrade.BackColor = Color.ForestGreen;
                btnUpgrade.Enabled = true;
            });
        }

        private void bwDownloader_DoWork(object sender, DoWorkEventArgs e)
        {
            DownloadOne(url, fileToSave, false);
        }

        private void btnUpgrade_Click(object sender, EventArgs e)
        {
            btnUpgrade.Enabled = false;
            btnUpgrade.BackColor = Color.Gray;
            btnUpgrade.Text = "Downloading ...";
            url = "https://exirmatab.com/uploads/Exir-latest.zip";
            //url = "https://exirmatab.com/wp-content/uploads/2021/03/1.jpg";
            fileToSave = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Exir-latest.zip");
            folderToSave = Path.Combine(System.IO.Directory.GetCurrentDirectory());
            string fileToRun = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Exir", "ExirSetup.exe");
            //string fileToRun2 = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Exir", "ExirSetup.exe");

            bwDownloader.RunWorkerAsync();

            if (DownloadComplete)
            {
                if (MessageBox.Show("Downloaded successfully.\n\n" +
                    "Please extract zip file then run ExirSetup.exe\n\nOpen it?", "Successful x:2965"
                    , MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {

                    if (!System.IO.File.Exists(fileToRun))
                    {
                        //System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
                        System.IO.Compression.ZipFile.ExtractToDirectory(fileToSave, folderToSave);

                        //, " /SILENT /ALLUSERS "
                        ProcessStartInfo procStartInfo = new ProcessStartInfo(fileToRun)
                        {
                            //Verb = "runas"
                        };

                        Process process = new Process();
                        process.StartInfo = procStartInfo;
                        process.EnableRaisingEvents = true;
                        process.Start();
                    }
                    else
                    {
                        ProcessStartInfo procStartInfo = new ProcessStartInfo(fileToSave)
                        {
                            //Verb = "runas"
                        };

                        Process process = new Process();
                        process.StartInfo = procStartInfo;
                        process.EnableRaisingEvents = true;
                        process.Start();
                    }

                    Application.Exit();
                }
            }
        }

    }
}
