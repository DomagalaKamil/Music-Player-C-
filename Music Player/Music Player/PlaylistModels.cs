namespace Music_Player;

public sealed class SongItem
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

public sealed class PlaylistItem
{
    public PlaylistItem(string name, string genre, List<SongItem> songs)
    {
        Name = name;
        Genre = genre;
        Songs = songs;
    }

    public string Name { get; }
    public string Genre { get; }
    public List<SongItem> Songs { get; set; }
}
