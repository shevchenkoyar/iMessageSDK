namespace iMessageSDK.Diagnostics;

/// <summary>
/// The result of checking a single macOS permission the SDK depends on.
/// </summary>
/// <param name="IsGranted">Whether the permission appears to be granted.</param>
/// <param name="Details">A human-readable explanation, useful when <paramref name="IsGranted"/> is <see langword="false"/>.</param>
public sealed record PermissionCheckResult(bool IsGranted, string? Details);
