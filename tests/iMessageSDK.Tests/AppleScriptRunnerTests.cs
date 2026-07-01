using iMessageSDK.Internal.Sending;

namespace iMessageSDK.Tests;

public class AppleScriptRunnerTests
{
    // These tests shell out to the real /usr/bin/osascript to prove that dynamic values reach the
    // script as literal argv entries rather than being interpolated into script text. They touch
    // no application state (Messages is never addressed), so they run unconditionally on macOS;
    // on any other OS they pass trivially, since the SDK itself only runs on macOS.

    [Fact]
    public async Task RunAsync_PassesArgumentsVerbatim_WithoutInjectionRisk()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        const string script = """
            on run argv
                return item 1 of argv
            end run
            """;

        const string tricky = "\" ; tell application \"Finder\" to activate -- ' \\ end tell";

        var result = await AppleScriptRunner.RunAsync(script, [tricky], CancellationToken.None);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Equal(tricky, result.StandardOutput.TrimEnd('\n'));
    }

    [Fact]
    public async Task RunAsync_PassesMultipleArguments_InOrder()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        const string script = """
            on run argv
                return (item 1 of argv) & "|" & (item 2 of argv)
            end run
            """;

        var result = await AppleScriptRunner.RunAsync(script, ["first", "second"], CancellationToken.None);

        Assert.True(result.Succeeded, result.StandardError);
        Assert.Equal("first|second", result.StandardOutput.TrimEnd('\n'));
    }
}
