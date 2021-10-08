using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for Download.xaml
    /// </summary>
    public partial class Download : UserControl, IDisposable
    {
        bool isDownloadDone = false;
        CancellationTokenSource cancellationTokenSource;
        public Download(ValueTask downloadTask, Progress<double> progress, CancellationTokenSource cancellationToken)
        {
            InitializeComponent();

            progress.ProgressChanged += (s, value) =>
                {
                    Console.WriteLine($"progress received {value}");
                    ProgressBar.Value = value;
                };
            cancellationTokenSource = cancellationToken;

            StartDownload(downloadTask);
        }

        private async void StartDownload(ValueTask downloadTask)
        {
            try
            {
                await downloadTask;
                isDownloadDone = true;
                //ClearButton.Visibility = Visibility.Visible;
                ClearButton.Content = "✅";
            }
            catch (TaskCanceledException e)
            {
                this.ProgressBar.Foreground = Brushes.Red;
            }
            catch (Exception e)
            {
                MessageBox.Show(Application.Current.MainWindow, e.Message, "Something wrong happened");
                this.ProgressBar.Foreground = Brushes.Red;
            }
            finally
            {

            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if(!isDownloadDone)
            {
                Console.WriteLine("Requested cancellation");
                cancellationTokenSource.Cancel();
            }
        }

        public virtual void Dispose()
        {
            if(!isDownloadDone)
            {
                cancellationTokenSource.Cancel();
            }

        }
    }
}
