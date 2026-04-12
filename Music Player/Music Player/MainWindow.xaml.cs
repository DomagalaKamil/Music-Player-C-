using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Music_Player
{
    public partial class MainWindow : Window
    {
        private readonly List<SongItem> _allSongs = new()
        {
            new SongItem("Blinding Lights", "The Weeknd", "3:20"),
            new SongItem("Levitating", "Dua Lipa", "3:23"),
            new SongItem("As It Was", "Harry Styles", "2:47"),
            new SongItem("Stay", "The Kid LAROI, Justin Bieber", "2:21"),
            new SongItem("Watermelon Sugar", "Harry Styles", "2:54"),
            new SongItem("Bad Habits", "Ed Sheeran", "3:50"),
            new SongItem("Dance The Night", "Dua Lipa", "2:56"),
            new SongItem("Cruel Summer", "Taylor Swift", "2:58")
        };

        public MainWindow()
        {
            InitializeComponent();
            PlaylistSongList.ItemsSource = _allSongs;
        }

        private void OpenPlaylistView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button playlistButton)
            {
                return;
            }

            var playlistName = playlistButton.Tag as string ?? playlistButton.Content?.ToString() ?? "Playlist";
            PlaylistTitleText.Text = playlistName;
            PlaylistSongList.ItemsSource = _allSongs;

            HomePanelView.Visibility = Visibility.Collapsed;
            PlaylistOverlayView.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistOverlayView.Visibility = Visibility.Collapsed;
            HomePanelView.Visibility = Visibility.Visible;
        }

        private void SongButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button songButton || songButton.Tag is not SongItem selectedSong)
            {
                return;
            }

            NowPlayingTitleText.Text = selectedSong.Title;
            NowPlayingArtistText.Text = selectedSong.Artist;
            BottomNowPlayingTitleText.Text = selectedSong.Title;
            BottomNowPlayingArtistText.Text = selectedSong.Artist;
        }

        private sealed class SongItem
        {
            public SongItem(string title, string artist, string duration)
            {
                Title = title;
                Artist = artist;
                Duration = duration;
            }

            public string Title { get; }
            public string Artist { get; }
            public string Duration { get; }
        }
    }
}
