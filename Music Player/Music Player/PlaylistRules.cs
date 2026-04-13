namespace Music_Player;

public static class PlaylistRules
{
    public const int VisibleHomeCards = 3;

    public static bool TryValidateNewPlaylistName(string? name, IEnumerable<PlaylistItem> existingPlaylists, out string errorMessage, string? currentPlaylistName = null)
    {
        var trimmedName = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = "Playlist name is required.";
            return false;
        }

        if (existingPlaylists.Any(p =>
                string.Equals(p.Name, trimmedName, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(p.Name, currentPlaylistName, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "A playlist with this name already exists.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public static int MoveCarouselLeft(int currentStart)
    {
        return Math.Max(0, currentStart - 1);
    }

    public static int MoveCarouselRight(int currentStart, int totalPlaylists, int visibleCards = VisibleHomeCards)
    {
        var maxStart = Math.Max(0, totalPlaylists - visibleCards);
        return Math.Min(maxStart, currentStart + 1);
    }

    public static bool HasMoreRight(int currentStart, int totalPlaylists, int visibleCards = VisibleHomeCards)
    {
        return currentStart + visibleCards < totalPlaylists;
    }
}
