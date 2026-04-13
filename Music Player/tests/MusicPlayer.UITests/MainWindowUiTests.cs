using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.IO;
using Music_Player;

namespace MusicPlayer.UITests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class MainWindowUiTests
{
    [SetUp]
    public void SetUp()
    {
        var tempSettingsFile = Path.Combine(Path.GetTempPath(), "music-player-tests", $"settings-{Guid.NewGuid():N}.json");
        Environment.SetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH", tempSettingsFile);
        Environment.SetEnvironmentVariable("MUSIC_PLAYER_TEST_AUTO_CONFIRM_DELETE", "1");
    }

    [TearDown]
    public void TearDown()
    {
        var path = Environment.GetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH", null);
        Environment.SetEnvironmentVariable("MUSIC_PLAYER_TEST_AUTO_CONFIRM_DELETE", null);

        if (!string.IsNullOrWhiteSpace(path))
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup issues in tests.
                }
            }
        }
    }

    [Test]
    public void HomeButton_ReturnsFromPlaylistOverlayToHome()
    {
        var window = new MainWindow();
        try
        {
            var allSongs = GetControl<Button>(window, "AllSongsSidebarButton");
            var homeButton = GetControl<Button>(window, "HomeSidebarButton");
            var playlistOverlay = GetControl<Border>(window, "PlaylistOverlayView");
            var homePanel = GetControl<ScrollViewer>(window, "HomePanelView");

            allSongs.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(playlistOverlay.Visibility, Is.EqualTo(Visibility.Visible));
            Assert.That(homePanel.Visibility, Is.EqualTo(Visibility.Collapsed));

            homeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(playlistOverlay.Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(homePanel.Visibility, Is.EqualTo(Visibility.Visible));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void CreatePlaylistModal_CreatesPlaylistAndAddsItToSidebar()
    {
        var window = new MainWindow();
        try
        {
            var openButton = GetControl<Button>(window, "OpenCreatePlaylistButton");
            var modal = GetControl<Border>(window, "CreatePlaylistModalOverlay");
            var nameInput = GetControl<TextBox>(window, "NewPlaylistNameInput");
            var createButton = GetControl<Button>(window, "CreatePlaylistConfirmButton");
            var sidebarPlaylists = GetControl<ItemsControl>(window, "SidebarPlaylistsItems");

            openButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(modal.Visibility, Is.EqualTo(Visibility.Visible));

            nameInput.Text = "Test Playlist";
            createButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(modal.Visibility, Is.EqualTo(Visibility.Collapsed));
            Assert.That(sidebarPlaylists.Items.Count, Is.EqualTo(1));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void EditPlaylist_UpdatesNameInSidebar()
    {
        var window = new MainWindow();
        try
        {
            CreatePlaylist(window, "Original Playlist");

            var sidebarPlaylists = GetControl<ItemsControl>(window, "SidebarPlaylistsItems");
            var playlist = (PlaylistItem)sidebarPlaylists.Items[0];

            InvokePrivateClick(window, "OpenEditPlaylistModal_Click", playlist);

            var nameInput = GetControl<TextBox>(window, "NewPlaylistNameInput");
            var createButton = GetControl<Button>(window, "CreatePlaylistConfirmButton");
            nameInput.Text = "Updated Playlist";
            createButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.That(sidebarPlaylists.Items.Count, Is.EqualTo(1));
            var updated = (PlaylistItem)sidebarPlaylists.Items[0];
            Assert.That(updated.Name, Is.EqualTo("Updated Playlist"));
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void DeleteActivePlaylist_RemovesItAndReturnsToAllSongs()
    {
        var window = new MainWindow();
        try
        {
            CreatePlaylist(window, "To Delete");

            var sidebarPlaylists = GetControl<ItemsControl>(window, "SidebarPlaylistsItems");
            var playlist = (PlaylistItem)sidebarPlaylists.Items[0];

            InvokePrivateClick(window, "SidebarPlaylistButton_Click", playlist);

            var playlistTitle = GetControl<TextBlock>(window, "PlaylistTitleText");
            Assert.That(playlistTitle.Text, Is.EqualTo("To Delete"));

            InvokePrivateClick(window, "DeletePlaylist_Click", playlist);

            Assert.That(sidebarPlaylists.Items.Count, Is.EqualTo(0));
            Assert.That(playlistTitle.Text, Is.EqualTo("All songs"));
        }
        finally
        {
            window.Close();
        }
    }

    private static void CreatePlaylist(MainWindow window, string playlistName)
    {
        var openButton = GetControl<Button>(window, "OpenCreatePlaylistButton");
        var nameInput = GetControl<TextBox>(window, "NewPlaylistNameInput");
        var createButton = GetControl<Button>(window, "CreatePlaylistConfirmButton");

        openButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        nameInput.Text = playlistName;
        createButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private static void InvokePrivateClick(MainWindow window, string methodName, PlaylistItem playlist)
    {
        var method = typeof(MainWindow).GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"Expected method '{methodName}' to exist.");

        var sender = new Button { Tag = playlist };
        method!.Invoke(window, new object[] { sender, new RoutedEventArgs(Button.ClickEvent) });
    }

    private static T GetControl<T>(FrameworkElement root, string name) where T : FrameworkElement
    {
        var control = root.FindName(name) as T;
        Assert.That(control, Is.Not.Null, $"Expected control '{name}' to exist.");
        return control!;
    }
}
