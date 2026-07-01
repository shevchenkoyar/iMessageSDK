namespace iMessageSDK.Internal.Attachments;

/// <summary>
/// Resolves the storage paths chat.db records for attachments (which are written with a leading
/// <c>~</c> referring to the owning user's home directory) to absolute filesystem paths.
/// </summary>
internal static class AttachmentPathResolver
{
    public static string? ResolveAbsolutePath(string? storedPath)
    {
        if (string.IsNullOrEmpty(storedPath))
        {
            return null;
        }

        if (storedPath.StartsWith("~/", StringComparison.Ordinal))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, storedPath[2..]);
        }

        return storedPath;
    }
}
