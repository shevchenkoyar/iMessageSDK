namespace iMessageSDK.Diagnostics;

/// <summary>
/// The availability of the Messages application on the local machine.
/// </summary>
/// <param name="IsInstalled">Whether the Messages application is installed.</param>
/// <param name="IsRunning">Whether the Messages application is currently running.</param>
public sealed record MessagesAppAvailability(bool IsInstalled, bool IsRunning);
