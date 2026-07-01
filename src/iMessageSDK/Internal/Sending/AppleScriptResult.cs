namespace iMessageSDK.Internal.Sending;

/// <summary>The raw outcome of invoking <c>osascript</c>.</summary>
/// <param name="ExitCode">The process exit code; zero indicates success.</param>
/// <param name="StandardOutput">Captured standard output.</param>
/// <param name="StandardError">Captured standard error, where AppleScript reports error numbers and messages.</param>
internal sealed record AppleScriptResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// <see langword="true"/> if the failure looks like Automation access being denied (AppleEvent
    /// error -1743, "not allowed").
    /// </summary>
    public bool IsAutomationPermissionDenied =>
        !Succeeded && (StandardError.Contains("-1743", StringComparison.Ordinal)
            || StandardError.Contains("not allowed", StringComparison.OrdinalIgnoreCase)
            || StandardError.Contains("not authorized", StringComparison.OrdinalIgnoreCase));
}
