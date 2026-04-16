using System;

namespace Music_Player;

public static class PlaybackRules
{
    public static int GetPreviousIndex(int currentIndex, int songCount, bool isShuffleEnabled, bool isRepeatEnabled, Random random)
    {
        if (songCount <= 0)
        {
            return -1;
        }

        if (isShuffleEnabled)
        {
            return GetRandomSongIndex(currentIndex, songCount, random);
        }

        if (currentIndex <= 0)
        {
            return isRepeatEnabled ? songCount - 1 : 0;
        }

        return currentIndex - 1;
    }

    public static int GetNextIndex(int currentIndex, int songCount, bool isShuffleEnabled, bool isRepeatEnabled, Random random)
    {
        if (songCount <= 0)
        {
            return -1;
        }

        if (isShuffleEnabled)
        {
            return GetRandomSongIndex(currentIndex, songCount, random);
        }

        if (currentIndex < 0)
        {
            return 0;
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex >= songCount)
        {
            return isRepeatEnabled ? 0 : -1;
        }

        return nextIndex;
    }

    public static int GetRandomSongIndex(int currentIndex, int songCount, Random random)
    {
        if (songCount <= 0)
        {
            return -1;
        }

        if (songCount == 1)
        {
            return 0;
        }

        int randomIndex;
        do
        {
            randomIndex = random.Next(0, songCount);
        }
        while (randomIndex == currentIndex);

        return randomIndex;
    }
}
