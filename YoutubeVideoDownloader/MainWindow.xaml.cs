using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Net.NetworkInformation;

namespace YoutubeVideoDownloader
{
    /// <summary>
    /// MainWindow.xaml etkileşim mantığı
    /// </summary>
    public partial class MainWindow : Window
    {
        private string path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();
        private string selectedURL = "";
        BackgroundWorker bg1 = new BackgroundWorker();
        string fileName = "youtube-dl.exe";
        string parameters;
        string islemsonuc = "";

        public MainWindow()
        {
            InitializeComponent();
            
            txt_dizin.Text = path;

            bg1.WorkerReportsProgress = true;
            bg1.WorkerSupportsCancellation = true;
            bg1.DoWork += new DoWorkEventHandler(bg1_DoWork);
            bg1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bg1_Complete);
            bg1.ProgressChanged += new ProgressChangedEventHandler(bg1_PrChange);
        }

        private void bg1_PrChange(object sender, ProgressChangedEventArgs e)
        {
            lbl_islem.Content = e.ProgressPercentage == 1 ? "İndirme Devam ediyor" : "yok";
            image_proses.Visibility = Visibility.Visible;
        }
        private void bg1_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
                lbl_islem.Content = "İptal edildi";
            else if (e.Error != null)
                lbl_islem.Content = "Error: " + e.Error.Message;
            else
            {
                btn_indir.IsEnabled = true;
                lbl_islem.Content = "İndirme Tamamlandı.";
                image_proses.Visibility = Visibility.Hidden;
            }
        }
        private void bg1_DoWork(object sender, DoWorkEventArgs e)
        {

            BackgroundWorker bgworker = sender as BackgroundWorker;
            ProcessStartInfo psstart = ProcInfoOlustur(fileName, parameters);
            using (Process pros = Process.Start(psstart))
            {
                pros.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                pros.Start();
                pros.BeginOutputReadLine();
                bgworker.ReportProgress(1, islemsonuc);
                pros.WaitForExit();
            }
        }
        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            islemsonuc += e.Data != null ? e.Data : "";
        }

        private ProcessStartInfo ProcInfoOlustur(string fileName, string parameters)
        {
            ProcessStartInfo process = new ProcessStartInfo();
            process.FileName = fileName;
            process.Arguments = parameters;
            process.UseShellExecute = false;
            process.RedirectStandardOutput = true;
            process.CreateNoWindow = true;

            return process;
        }

        private void btn_indir_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_format.SelectedItem == null) return;

            

            if (cmb_format.SelectedIndex != -1)
            {
                string selectedFormat = cmb_format.SelectedItem.ToString();
                string code = selectedFormat.Split(' ').ToArray()[0].ToString();
                parameters = $"-o {path}\\%(title)s.%(ext)s -f {code} {selectedURL}";
                ((Button)sender).IsEnabled = false;
                if (!bg1.IsBusy)
                {
                    bg1.RunWorkerAsync();
                }
                //ProcessStartInfo prcInfo = ProcInfoOlustur(fileName, parameters);
                //using (Process proc = Process.Start(prcInfo))
                //{
                //    proc.WaitForExit();
                //}

                
                    
            }
            else
            {
                MessageBox.Show("Lütfen hangi formatta indireceğinizi seçiniz");
            }
            //ProcessStartInfo prcInfo = ProcInfoOlustur(fileName, parameters);
            //using (Process proc = Process.Start(prcInfo))
            //{
            //    proc.WaitForExit();
            //    MessageBox.Show("İndirme tamamlandı!", "", MessageBoxButton.OK, MessageBoxImage.Information);
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string link = Clipboard.GetText();
            int a = link.IndexOf("youtube");
            if (a > 7)
            {
                txt_url.Text = link;
            }
            selectedURL = txt_url.Text;
            string parameters = "-F " + selectedURL;
            string parameters2 = "--skip-download --get-title --get-thumbnail --get-duration --get-filename " + selectedURL;


            if (InterNetKontrol(selectedURL))
            {
                ProcessStartInfo procInfo = ProcInfoOlustur(fileName, parameters2);
                using (Process proc = Process.Start(procInfo))
                {
                    cmb_format.Items.Clear();
                    proc.WaitForExit();
                    byte s = 0;
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string sonuc = proc.StandardOutput.ReadLine();
                        if (s == 0)
                        {
                            string ss = "Adı: ";
                            if (sonuc.Length > 50)
                            {
                                ss += sonuc.Substring(0, 50);
                                ss += ("\n" + sonuc.Substring(50));
                            }
                            else
                                ss += sonuc;

                            lbl_isim.Content = ss;
                        }
                        if (s == 1)
                        {
                            BitmapImage nbmp = new BitmapImage();
                            nbmp.BeginInit();
                            nbmp.UriSource = new Uri(sonuc, UriKind.Absolute);
                            nbmp.EndInit();
                            image.Source = nbmp;
                        }
                        if (s == 2)
                            lbl_isim.Content += ("\nDosya: " + sonuc);
                        if (s++ == 3)
                            lbl_isim.Content += ("\nSüre: " + sonuc);    
                    }
                }
                procInfo = ProcInfoOlustur(fileName, parameters);
                using (Process pros = Process.Start(procInfo))
                {
                    lbl_islem.Content = "Sonuçlar alınıyor bekleyin!!!";
                    pros.Start();
                    pros.WaitForExit();
                    cmb_format.Items.Clear();
                    int s = 0;
                    while (!pros.StandardOutput.EndOfStream)
                    {

                        string sonuc = pros.StandardOutput.ReadLine();
                        if (s++ > 2)
                            cmb_format.Items.Add(sonuc);

                    }
                    cmb_format.IsEnabled = true;
                    lbl_islem.Content = "Sonuçar alındı. İndirmek için seçiminizi yapın";
                    lbl_islem.Foreground = Brushes.Green;
                }
            }
            else
            {
                MessageBox.Show("Bu internet Adresine ulaşılamıyor");
            }
                
        }

        private bool InterNetKontrol(string filename)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
                return true;
            else
                return false;
        }

            private void btn_dizin_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = path;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                path = dialog.FileName;
                txt_dizin.Text = path;
            }
        }
    }
}
