using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace eChess
{
    /// <summary>
    /// Interaction logic for Updater.xaml
    /// </summary>
    public partial class Updater : Page
    {
        static readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\eChess\\";
        static HttpClient client = new HttpClient();
        static string result = string.Empty;
        static string latestVersion = string.Empty;
        int progress = 0;
        string URL = string.Empty;

        public Updater()
        {
            InitializeComponent();
        }

        public bool NewVersionAvailable(string currentVersion)
        {
            client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0");
            result = client.GetAsync("https://api.github.com/repos/SagMeinenNamen/eChess/releases").Result.Content.ReadAsStringAsync().Result;
            latestVersion = GetTagName();

            if (latestVersion != currentVersion)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DownloadUpate()
        {
            URL = GetDownloadURL();
            if (!File.Exists(path + latestVersion + "\\eChess.exe"))
            {
                try
                {
                    Directory.CreateDirectory(path + latestVersion);
                    WebClient wc = new WebClient();
                    wc.DownloadFileCompleted += Wc_DownloadFileCompleted;
                    wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(new Uri(URL), path + latestVersion + "\\eChess.exe");
                 
                }
                catch (Exception e)
                {
                    MessageBox.Show("Update failed: " + e.Message);
                }
            }
            else
            {
                Process.Start(path + latestVersion + "\\eChess.exe");
                Environment.Exit(0);
            }
        }


        private void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > progress)
            {
                Dispatcher.BeginInvoke(new Action(() => DownloadProgress.Value = e.ProgressPercentage));
                progress = e.ProgressPercentage;
            }
        }

        private static void Wc_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Process.Start(path + latestVersion + "\\eChess.exe");
            Environment.Exit(0);
        }

        static string GetDownloadURL()
        {
            string propName = $"\"browser_download_url\": \"";
            var url = result.Substring(result.IndexOf(propName) + propName.Length);
            url = url.Substring(0, url.IndexOf("\""));
            return url;
        }

        static string GetTagName()
        {
            string propName = $"\"tag_name\": \"";
            var tagName = result.Substring(result.IndexOf(propName) + propName.Length);
            tagName = tagName.Substring(0, tagName.IndexOf("\""));
            return tagName;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DownloadUpate();
        }
    }
}
