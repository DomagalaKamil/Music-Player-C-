using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Music_Player
{
    public partial class MainWindow : Window
    {
        private const string SettingsFileName = "settings.json";

        private static readonly HashSet<string> SupportedMusicExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".mp4", ".m4a", ".wav", ".flac", ".aac", ".ogg", ".wma", ".opus", ".aiff", ".alac"
        };

        private static readonly string[] HomeCardColorPalette =
        {
            "#3D3D3D", "#3A5647", "#5B3A3A", "#44556B", "#5D3D5D", "#556048"
        };

        private string? _musicFolderPath;
        private List<SongItem> _allSongs = new();
        private readonly List<PlaylistItem> _userPlaylists = new();
        private PlaylistItem? _editingPlaylist;
        private int _homeCarouselStartIndex;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettingsAndLibrary();

            PlaylistSongList.ItemsSource = _allSongs;
            NewPlaylistSongsList.ItemsSource = _allSongs;
            SidebarPlaylistsItems.ItemsSource = _userPlaylists;

            RefreshHomePlaylistCards();
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

            _musicFolderPath = folderDialog.FolderName;
            _allSongs = LoadSongsFromFolder(_musicFolderPath);

            RebindPlaylistSongsToLibrary();

            PlaylistSongList.ItemsSource = _allSongs;
            NewPlaylistSongsList.ItemsSource = _allSongs;

            OpenPlaylistView("All songs", _allSongs);
            RefreshHomePlaylistCards();
            SaveSettings();

            if (_allSongs.Count == 0)
            {
                MessageBox.Show(
                    "No supported music files were found in the selected folder.",
                    "Music Player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void OpenAllSongsPlaylist_Click(object sender, RoutedEventArgs e)
        {
            OpenPlaylistView("All songs", _allSongs);
        }

        private void SidebarPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: PlaylistItem playlist })
            {
                return;
            }

            OpenPlaylistView(playlist.Name, playlist.Songs);
        }

        private void HomePlaylistCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: PlaylistItem playlist })
            {
                return;
            }

            OpenPlaylistView(playlist.Name, playlist.Songs);
        }

        private void OpenPlaylistView(string title, List<SongItem> songs)
        {
            PlaylistTitleText.Text = title;
            PlaylistSongList.ItemsSource = songs;

            HomePanelView.Visibility = Visibility.Collapsed;
            PlaylistOverlayView.Visibility = Visibility.Visible;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            PlaylistOverlayView.Visibility = Visibility.Collapsed;
            HomePanelView.Visibility = Visibility.Visible;
        }

        private void OpenCreatePlaylistModal_Click(object sender, RoutedEventArgs e)
        {
            _editingPlaylist = null;
            CreatePlaylistModalTitle.Text = "Create Playlist";
            CreatePlaylistConfirmButton.Content = "Create Playlist";
            NewPlaylistNameInput.Text = string.Empty;
            NewPlaylistGenreInput.Text = string.Empty;
            NewPlaylistSongsList.ItemsSource = _allSongs;
            NewPlaylistSongsList.SelectedItems.Clear();
            CreatePlaylistModalOverlay.Visibility = Visibility.Visible;
        }

        private void OpenEditPlaylistModal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: PlaylistItem playlist })
            {
                return;
            }

            _editingPlaylist = playlist;
            CreatePlaylistModalTitle.Text = "Update Playlist";
            CreatePlaylistConfirmButton.Content = "Update Playlist";
            NewPlaylistNameInput.Text = playlist.Name;
            NewPlaylistGenreInput.Text = playlist.Genre;
            NewPlaylistSongsList.ItemsSource = _allSongs;
            NewPlaylistSongsList.SelectedItems.Clear();

            var selectedPaths = playlist.Songs.Select(song => song.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var song in _allSongs.Where(song => selectedPaths.Contains(song.FilePath)))
            {
                NewPlaylistSongsList.SelectedItems.Add(song);
            }

            CreatePlaylistModalOverlay.Visibility = Visibility.Visible;
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: PlaylistItem playlist })
            {
                return;
            }

            var autoConfirmDelete = string.Equals(
                Environment.GetEnvironmentVariable("MUSIC_PLAYER_TEST_AUTO_CONFIRM_DELETE"),
                "1",
                StringComparison.Ordinal);

            var confirm = autoConfirmDelete
                ? MessageBoxResult.Yes
                : MessageBox.Show(
                    $"Delete playlist '{playlist.Name}'?",
                    "Music Player",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _userPlaylists.Remove(playlist);
            _editingPlaylist = null;
            ClampCarouselStart();
            SidebarPlaylistsItems.Items.Refresh();
            RefreshHomePlaylistCards();
            SaveSettings();

            if (string.Equals(PlaylistTitleText.Text, playlist.Name, StringComparison.OrdinalIgnoreCase))
            {
                OpenPlaylistView("All songs", _allSongs);
            }
        }

        private void CloseCreatePlaylistModal_Click(object sender, RoutedEventArgs e)
        {
            _editingPlaylist = null;
            CreatePlaylistModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void CreatePlaylist_Click(object sender, RoutedEventArgs e)
        {
            var playlistName = NewPlaylistNameInput.Text.Trim();
            var playlistGenre = NewPlaylistGenreInput.Text.Trim();

            if (!PlaylistRules.TryValidateNewPlaylistName(playlistName, _userPlaylists, out var validationMessage, _editingPlaylist?.Name))
            {
                MessageBox.Show(
                    validationMessage,
                    "Music Player",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectedSongs = NewPlaylistSongsList.SelectedItems
                .OfType<SongItem>()
                .ToList();

            if (_editingPlaylist is null)
            {
                _userPlaylists.Add(new PlaylistItem(playlistName, playlistGenre, selectedSongs));

                if (_userPlaylists.Count > PlaylistRules.VisibleHomeCards)
                {
                    _homeCarouselStartIndex = _userPlaylists.Count - PlaylistRules.VisibleHomeCards;
                }
            }
            else
            {
                var existingIndex = _userPlaylists.IndexOf(_editingPlaylist);
                if (existingIndex >= 0)
                {
                    _userPlaylists[existingIndex] = new PlaylistItem(playlistName, playlistGenre, selectedSongs);
                }

                if (string.Equals(PlaylistTitleText.Text, _editingPlaylist.Name, StringComparison.OrdinalIgnoreCase))
                {
                    OpenPlaylistView(playlistName, selectedSongs);
                }

                _editingPlaylist = null;
            }

            ClampCarouselStart();
            SidebarPlaylistsItems.Items.Refresh();
            RefreshHomePlaylistCards();
            SaveSettings();

            CreatePlaylistModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void HomeCarouselLeftButton_Click(object sender, RoutedEventArgs e)
        {
            _homeCarouselStartIndex = PlaylistRules.MoveCarouselLeft(_homeCarouselStartIndex);
            RefreshHomePlaylistCards();
        }

        private void HomeCarouselRightButton_Click(object sender, RoutedEventArgs e)
        {
            _homeCarouselStartIndex = PlaylistRules.MoveCarouselRight(_homeCarouselStartIndex, _userPlaylists.Count);
            RefreshHomePlaylistCards();
        }

        private void RefreshHomePlaylistCards()
        {
            var cards = new[]
            {
                new HomeCard(HomePlaylistCard1, HomePlaylistTitle1, HomePlaylistGenre1, HomePlaylistArt1),
                new HomeCard(HomePlaylistCard2, HomePlaylistTitle2, HomePlaylistGenre2, HomePlaylistArt2),
                new HomeCard(HomePlaylistCard3, HomePlaylistTitle3, HomePlaylistGenre3, HomePlaylistArt3)
            };

            for (var i = 0; i < cards.Length; i++)
            {
                var playlistIndex = _homeCarouselStartIndex + i;
                var card = cards[i];

                if (playlistIndex >= _userPlaylists.Count)
                {
                    card.Button.Visibility = Visibility.Collapsed;
                    continue;
                }

                var playlist = _userPlaylists[playlistIndex];
                card.Button.Visibility = Visibility.Visible;
                card.Button.Tag = playlist;
                card.Title.Text = playlist.Name;
                card.Genre.Text = playlist.Genre;
                card.Genre.Visibility = string.IsNullOrWhiteSpace(playlist.Genre) ? Visibility.Collapsed : Visibility.Visible;
                card.Art.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(HomeCardColorPalette[playlistIndex % HomeCardColorPalette.Length]));
            }

            HomeCarouselLeftButton.Visibility = _homeCarouselStartIndex > 0 ? Visibility.Visible : Visibility.Hidden;
            HomeCarouselRightButton.Visibility = PlaylistRules.HasMoreRight(_homeCarouselStartIndex, _userPlaylists.Count) ? Visibility.Visible : Visibility.Hidden;
        }

        private void ClampCarouselStart()
        {
            var maxStart = Math.Max(0, _userPlaylists.Count - PlaylistRules.VisibleHomeCards);
            _homeCarouselStartIndex = Math.Min(_homeCarouselStartIndex, maxStart);
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

        private void LoadSettingsAndLibrary()
        {
            var settings = ReadSettings();
            _musicFolderPath = settings.MusicFolderPath;

            if (!string.IsNullOrWhiteSpace(_musicFolderPath) && Directory.Exists(_musicFolderPath))
            {
                _allSongs = LoadSongsFromFolder(_musicFolderPath);
            }
            else
            {
                _allSongs = new List<SongItem>();
            }

            var songMap = _allSongs.ToDictionary(song => song.FilePath, StringComparer.OrdinalIgnoreCase);
            foreach (var storedPlaylist in settings.Playlists)
            {
                if (string.IsNullOrWhiteSpace(storedPlaylist.Name))
                {
                    continue;
                }

                var playlistSongs = storedPlaylist.SongPaths
                    .Where(path => songMap.ContainsKey(path))
                    .Select(path => songMap[path])
                    .ToList();

                _userPlaylists.Add(new PlaylistItem(storedPlaylist.Name.Trim(), storedPlaylist.Genre?.Trim() ?? string.Empty, playlistSongs));
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    MusicFolderPath = _musicFolderPath,
                    Playlists = _userPlaylists.Select(playlist => new StoredPlaylist
                    {
                        Name = playlist.Name,
                        Genre = playlist.Genre,
                        SongPaths = playlist.Songs.Select(song => song.FilePath).ToList()
                    }).ToList()
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.IO.File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch (Exception)
            {
                // Ignore save failures to keep the app responsive.
            }
        }

        private static AppSettings ReadSettings()
        {
            try
            {
                var settingsPath = GetSettingsFilePath();
                if (!System.IO.File.Exists(settingsPath))
                {
                    return new AppSettings();
                }

                var json = System.IO.File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        private static string GetSettingsFilePath()
        {
            var overridePath = Environment.GetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                var resolvedPath = Path.GetFullPath(overridePath);
                var resolvedDir = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrWhiteSpace(resolvedDir))
                {
                    Directory.CreateDirectory(resolvedDir);
                }

                return resolvedPath;
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "Music Player");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

        private void RebindPlaylistSongsToLibrary()
        {
            var songMap = _allSongs.ToDictionary(song => song.FilePath, StringComparer.OrdinalIgnoreCase);
            foreach (var playlist in _userPlaylists)
            {
                playlist.Songs = playlist.Songs
                    .Where(song => songMap.ContainsKey(song.FilePath))
                    .Select(song => songMap[song.FilePath])
                    .ToList();
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
                // Keep fallback values when metadata cannot be read.
            }

            return new SongItem(title, artist, duration, filePath);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"m\:ss");
        }

        private sealed class HomeCard
        {
            public HomeCard(Button button, TextBlock title, TextBlock genre, Border art)
            {
                Button = button;
                Title = title;
                Genre = genre;
                Art = art;
            }

            public Button Button { get; }
            public TextBlock Title { get; }
            public TextBlock Genre { get; }
            public Border Art { get; }
        }

        private sealed class StoredPlaylist
        {
            public string Name { get; init; } = string.Empty;
            public string Genre { get; init; } = string.Empty;
            public List<string> SongPaths { get; init; } = new();
        }

        private sealed class AppSettings
        {
            public string? MusicFolderPath { get; init; }
            public List<StoredPlaylist> Playlists { get; init; } = new();
        }
    }
}
