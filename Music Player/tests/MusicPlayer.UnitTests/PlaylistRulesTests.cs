using Music_Player;

namespace MusicPlayer.UnitTests;

[TestFixture]
public class PlaylistRulesTests
{
    [Test]
    public void TryValidateNewPlaylistName_ReturnsFalse_WhenNameIsEmpty()
    {
        var result = PlaylistRules.TryValidateNewPlaylistName("   ", new List<PlaylistItem>(), out var message);

        Assert.That(result, Is.False);
        Assert.That(message, Is.EqualTo("Playlist name is required."));
    }

    [Test]
    public void TryValidateNewPlaylistName_ReturnsFalse_WhenDuplicateExists()
    {
        var existing = new List<PlaylistItem>
        {
            new("Road Trip", "Pop", new List<SongItem>())
        };

        var result = PlaylistRules.TryValidateNewPlaylistName("road trip", existing, out var message);

        Assert.That(result, Is.False);
        Assert.That(message, Is.EqualTo("A playlist with this name already exists."));
    }

    [Test]
    public void TryValidateNewPlaylistName_AllowsSameName_WhenEditingCurrentPlaylist()
    {
        var existing = new List<PlaylistItem>
        {
            new("Road Trip", "Pop", new List<SongItem>())
        };

        var result = PlaylistRules.TryValidateNewPlaylistName(
            "Road Trip",
            existing,
            out var message,
            currentPlaylistName: "Road Trip");

        Assert.That(result, Is.True);
        Assert.That(message, Is.EqualTo(string.Empty));
    }

    [Test]
    public void TryValidateNewPlaylistName_RejectsRenamingToAnotherExistingPlaylist()
    {
        var existing = new List<PlaylistItem>
        {
            new("Road Trip", "Pop", new List<SongItem>()),
            new("Evening Mix", "Chill", new List<SongItem>())
        };

        var result = PlaylistRules.TryValidateNewPlaylistName(
            "Road Trip",
            existing,
            out var message,
            currentPlaylistName: "Evening Mix");

        Assert.That(result, Is.False);
        Assert.That(message, Is.EqualTo("A playlist with this name already exists."));
    }

    [Test]
    public void TryValidateNewPlaylistName_ReturnsTrue_ForUniqueName()
    {
        var existing = new List<PlaylistItem>
        {
            new("Road Trip", "Pop", new List<SongItem>())
        };

        var result = PlaylistRules.TryValidateNewPlaylistName("Evening Mix", existing, out var message);

        Assert.That(result, Is.True);
        Assert.That(message, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MoveCarouselLeft_DoesNotGoBelowZero()
    {
        Assert.That(PlaylistRules.MoveCarouselLeft(0), Is.EqualTo(0));
        Assert.That(PlaylistRules.MoveCarouselLeft(3), Is.EqualTo(2));
    }

    [Test]
    public void MoveCarouselRight_ClampsToMaximumStart()
    {
        Assert.That(PlaylistRules.MoveCarouselRight(0, totalPlaylists: 2), Is.EqualTo(0));
        Assert.That(PlaylistRules.MoveCarouselRight(0, totalPlaylists: 5), Is.EqualTo(1));
        Assert.That(PlaylistRules.MoveCarouselRight(2, totalPlaylists: 5), Is.EqualTo(2));
    }

    [Test]
    public void HasMoreRight_ReturnsExpectedValue()
    {
        Assert.That(PlaylistRules.HasMoreRight(0, totalPlaylists: 2), Is.False);
        Assert.That(PlaylistRules.HasMoreRight(0, totalPlaylists: 4), Is.True);
        Assert.That(PlaylistRules.HasMoreRight(1, totalPlaylists: 4), Is.False);
    }
}
