using Music_Player;

namespace MusicPlayer.UnitTests;

[TestFixture]
public class PlaybackRulesTests
{
    [Test]
    public void GetNextIndex_ReturnsMinusOne_WhenSongListIsEmpty()
    {
        var result = PlaybackRules.GetNextIndex(
            currentIndex: 0,
            songCount: 0,
            isShuffleEnabled: false,
            isRepeatEnabled: false,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void GetNextIndex_ReturnsZero_WhenCurrentIndexIsBeforeStart()
    {
        var result = PlaybackRules.GetNextIndex(
            currentIndex: -1,
            songCount: 5,
            isShuffleEnabled: false,
            isRepeatEnabled: false,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetNextIndex_ReturnsMinusOne_AtEnd_WhenRepeatIsDisabled()
    {
        var result = PlaybackRules.GetNextIndex(
            currentIndex: 4,
            songCount: 5,
            isShuffleEnabled: false,
            isRepeatEnabled: false,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void GetNextIndex_WrapsToStart_AtEnd_WhenRepeatIsEnabled()
    {
        var result = PlaybackRules.GetNextIndex(
            currentIndex: 4,
            songCount: 5,
            isShuffleEnabled: false,
            isRepeatEnabled: true,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetPreviousIndex_ReturnsZero_WhenAtStart_AndRepeatIsDisabled()
    {
        var result = PlaybackRules.GetPreviousIndex(
            currentIndex: 0,
            songCount: 5,
            isShuffleEnabled: false,
            isRepeatEnabled: false,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetPreviousIndex_WrapsToLast_WhenAtStart_AndRepeatIsEnabled()
    {
        var result = PlaybackRules.GetPreviousIndex(
            currentIndex: 0,
            songCount: 5,
            isShuffleEnabled: false,
            isRepeatEnabled: true,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void ShuffleMode_ReturnsDifferentIndex_WhenMultipleSongsAvailable()
    {
        var next = PlaybackRules.GetNextIndex(
            currentIndex: 2,
            songCount: 6,
            isShuffleEnabled: true,
            isRepeatEnabled: false,
            random: new Random(123));

        Assert.That(next, Is.InRange(0, 5));
        Assert.That(next, Is.Not.EqualTo(2));
    }

    [Test]
    public void GetRandomSongIndex_ReturnsZero_WhenOnlyOneSong()
    {
        var result = PlaybackRules.GetRandomSongIndex(
            currentIndex: 0,
            songCount: 1,
            random: new Random(123));

        Assert.That(result, Is.EqualTo(0));
    }
}
