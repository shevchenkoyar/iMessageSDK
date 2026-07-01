namespace iMessageSDK;

/// <summary>
/// Options controlling how an <see cref="IMessageClient"/> connects to and observes Messages.
/// </summary>
public sealed class MessageClientOptions
{
    /// <summary>
    /// An override path to the Messages conversation history file. When <see langword="null"/>
    /// (the default), the standard location under the current user's home directory is used.
    /// </summary>
    /// <remarks>
    /// Primarily intended for pointing the SDK at a fixture database in tests; production
    /// consumers should leave this unset.
    /// </remarks>
    public string? MessagesDatabasePath { get; set; }

    /// <summary>
    /// The interval at which an <see cref="Watching.IMessageWatcher"/> falls back to polling for
    /// changes when it does not receive a more immediate change notification. Defaults to two
    /// seconds.
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);
}
