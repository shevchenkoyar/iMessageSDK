using System.Runtime.Versioning;

// The whole suite requires macOS (it exercises the real Messages conversation history format and,
// in a couple of cases, invokes /usr/bin/osascript directly), matching the SDK's own platform
// constraint.
[assembly: SupportedOSPlatform("macos")]
