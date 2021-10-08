using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly YoutubeClient youtube = new() { };
        bool isHighQuality = true;
        List<YoutubeExplode.Search.VideoSearchResult> currentSearchResults = new();
        //TODO: store a list of downloads and display them on the left 

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SearchInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Search();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void DisplaySearchResults()
        {
            ResultSpaceGrid.Children.Clear();

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var searchResult in currentSearchResults)
            {
                var thumbnail = searchResult.Thumbnails[0];

                var resultWidget = new SearchResultControl
                {
                    VideoTitleLabel = { Content = searchResult.Title },
                    VideoAuthorLabel = { Content = searchResult.Author },
                    ThumnailContainer =
                    {
                        Source = new BitmapImage(new Uri(thumbnail.Url))
                    },
                };

                resultWidget.Button.Click += (s, e) =>
                {
                    SelectVideo(searchResult);
                };


                stackPanel.Children.Add(resultWidget);
            }

            var scrollView = new ScrollViewer
            {
                Content = stackPanel
            };

            ResultSpaceGrid.Children.Add(scrollView);

        }

        private void ShowIndeterminateProgressBar()
        {
            ResultSpaceGrid.Children.Clear();

            var progressBar = new ProgressBar
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsIndeterminate = true,
                Width = 150,
                Height = 25,
            };

            _ = ResultSpaceGrid.Children.Add(progressBar);

        }


        private async void Search()
        {
            var searchInput = SearchInputBox.Text;

            var asyncIterator = youtube.Search.GetVideosAsync(searchInput).GetAsyncEnumerator();

            var results = new List<YoutubeExplode.Search.VideoSearchResult> { };

            const int maxResults = 10;

            ShowIndeterminateProgressBar();

            try
            {
                int i = 0;
                while (i < maxResults && await asyncIterator.MoveNextAsync())
                {
                    var searchResult = asyncIterator.Current;
                    results.Add(searchResult);
                    ++i;
                }
            }
            finally { if (asyncIterator != null) await asyncIterator.DisposeAsync(); }

            this.currentSearchResults = results;

            DisplaySearchResults();

        }



        private async void SelectVideo(YoutubeExplode.Search.VideoSearchResult searchResult)
        {
            // TODO: this takes time 
            ShowIndeterminateProgressBar();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(searchResult.Url);
            watch.Stop();
            Console.WriteLine($"getting stream manifest took {watch.Elapsed.TotalMilliseconds}ms");

            var streamInfo = isHighQuality ?
                streamManifest.GetMuxedStreams().GetWithHighestVideoQuality() :
                streamManifest.GetMuxedStreams().Aggregate((a, b) =>
                    a.Size < b.Size ? a : b
                );

            Console.WriteLine($"is high quality? : {isHighQuality}");

            // removes invalid characters
            var validFileName = new string(searchResult.Title.Where((ch) =>
                !System.IO.Path.GetInvalidFileNameChars().Contains(ch)
            ).ToArray());


            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = validFileName,
                DefaultExt = $".{streamInfo.Container.Name}",
            };

            //this is the owner (this window)
            var result = dialog.ShowDialog(this);

            if (result is null || result is false)
                return;

            var fileStream = dialog.OpenFile();

            ResultSpaceGrid.Children.Clear();

            var progressBar = new ProgressBar
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 150,
                Height = 25,
                Minimum = 0,
                Maximum = 1,
            };

            _ = ResultSpaceGrid.Children.Add(progressBar);

            var progress = new Progress<double>();
            var cancelToken = new System.Threading.CancellationTokenSource();
            var task = this.youtube.Videos.Streams.CopyToAsync(streamInfo, fileStream, progress, cancelToken.Token);

            var download = new Download(task, progress, cancelToken);

            download.DownloadName.Content = searchResult.Title;

            download.ClearButton.Click += (sender, ev) =>
            {
                RemoveDownloadFromQueue(download);
            };

            AddDownloadToQueue(download);

            DisplaySearchResults();
        }
        
        private void AddDownloadToQueue(Download download)
        {
            OnGoingDownloadsStackPanel.Children.Add(download);
            LeftPane.Visibility = Visibility.Visible;
        }
        private void RemoveDownloadFromQueue(Download download)
        {
            OnGoingDownloadsStackPanel.Children.Remove(download);

            if (OnGoingDownloadsStackPanel.Children.Count == 0)
            {
                LeftPane.Visibility = Visibility.Collapsed;
            }

        }

        private void RadioButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            var radio = (RadioButton)sender;
            var isChecked = radio.IsChecked ?? false;
            isHighQuality = isChecked;
        }

    }
}
