using System.Diagnostics;

namespace iMessageSDK.Internal.Sending;

/// <summary>
/// Invokes <c>osascript</c> to run an AppleScript against the Messages application.
/// </summary>
internal static class AppleScriptRunner
{
    private const string OsaScriptPath = "/usr/bin/osascript";

    public static async Task<AppleScriptResult> RunAsync(string script, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = OsaScriptPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(script);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var standardOutput = await standardOutputTask.ConfigureAwait(false);
        var standardError = await standardErrorTask.ConfigureAwait(false);

        return new AppleScriptResult(process.ExitCode, standardOutput, standardError);
    }
}
