using System.Windows;
using System.Windows.Controls;
using System.Threading;
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
    }

    [TearDown]
    public void TearDown()
    {
        var path = Environment.GetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH");
        Environment.SetEnvironmentVariable("MUSIC_PLAYER_SETTINGS_PATH", null);

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

    private static T GetControl<T>(FrameworkElement root, string name) where T : FrameworkElement
    {
        var control = root.FindName(name) as T;
        Assert.That(control, Is.Not.Null, $"Expected control '{name}' to exist.");
        return control!;
    }
}
