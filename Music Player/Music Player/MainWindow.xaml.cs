using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using TagLib;

namespace Music_Player
{
    public partial class MainWindow : Window
    {
        private const string SettingsFileName = "settings.json";
        private static readonly HashSet<string> SupportedMusicExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".mp4", ".m4a", ".wav", ".flac", ".aac", ".ogg", ".wma", ".opus", ".aiff", ".alac"
        };

        private List<SongItem> _allSongs = new();

        public MainWindow()
        {
            InitializeComponent();
            PlaylistSongList.ItemsSource = _allSongs;
            TryLoadSavedMusicFolder();
        }

        private void UploadMusic_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select your music folder"
            };

            if (folderDialog.ShowDialog() != true)
            {
                return;
            }

            var loadedSongs = LoadSongsFromFolder(folderDialog.FolderName);
            _allSongs = loadedSongs;
            PlaylistSongList.ItemsSource = _allSongs;
            SaveMusicFolderPath(folderDialog.FolderName);

            PlaylistTitleText.Text = "All songs";
            HomePanelView.Visibility = Visibility.Collapsed;
            PlaylistOverlayView.Visibility = Visibility.Visible;

            if (_allSongs.Count == 0)
            {
                MessageBox.Show(
                    "No supported music files were found in the selected folder.",
                    "Music Player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
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

        private static string GetSettingsFilePath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "Music Player");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

        private void SaveMusicFolderPath(string folderPath)
        {
            try
            {
                var settings = new AppSettings { MusicFolderPath = folderPath };
                var json = JsonSerializer.Serialize(settings);
                System.IO.File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch (Exception)
            {
                // Ignore save failures to avoid blocking the main flow.
            }
        }

        private void TryLoadSavedMusicFolder()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (!System.IO.File.Exists(settingsPath))
                {
                    return;
                }

                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings == null || string.IsNullOrWhiteSpace(settings.MusicFolderPath))
                {
                    return;
                }

                if (!Directory.Exists(settings.MusicFolderPath))
                {
                    return;
                }

                _allSongs = LoadSongsFromFolder(settings.MusicFolderPath);
                PlaylistSongList.ItemsSource = _allSongs;
            }
            catch (Exception)
            {
                // Ignore load failures and continue with empty list.
            }
        }

        private static List<SongItem> LoadSongsFromFolder(string folderPath)
        {
            var songs = new List<SongItem>();

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(path => SupportedMusicExtensions.Contains(Path.GetExtension(path)));
            }
            catch (Exception)
            {
                return songs;
            }

            foreach (var filePath in files)
            {
                songs.Add(ReadSongFromFile(filePath));
            }

            return songs
                .OrderBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static SongItem ReadSongFromFile(string filePath)
        {
            var title = Path.GetFileNameWithoutExtension(filePath);
            var artist = "N/A";
            var duration = "--:--";

            try
            {
                using var tagFile = TagLib.File.Create(filePath);

                if (!string.IsNullOrWhiteSpace(tagFile.Tag.Title))
                {
                    title = tagFile.Tag.Title.Trim();
                }

                var performer = tagFile.Tag.FirstPerformer;
                if (!string.IsNullOrWhiteSpace(performer))
                {
                    artist = performer.Trim();
                }

                if (tagFile.Properties.Duration.TotalSeconds > 0)
                {
                    duration = FormatDuration(tagFile.Properties.Duration);
                }
            }
            catch (Exception)
            {
                // Keep fallback title/artist/duration values when metadata cannot be read.
            }

            return new SongItem(title, artist, duration, filePath);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"m\:ss");
        }

        private sealed class SongItem
        {
            public SongItem(string title, string artist, string duration, string filePath)
            {
                Title = title;
                Artist = artist;
                Duration = duration;
                FilePath = filePath;
            }

            public string Title { get; }
            public string Artist { get; }
            public string Duration { get; }
            public string FilePath { get; }
        }

        private sealed class AppSettings
        {
            public string? MusicFolderPath { get; init; }
        }
    }
}
