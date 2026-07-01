namespace iMessageSDK.Diagnostics;

/// <summary>
/// The health of the Messages conversation history the SDK reads from.
/// </summary>
/// <remarks>
/// This is a deliberate, narrow exception to the SDK's domain-first design: it exists purely to
/// help diagnose setup problems (missing file, unreadable file, an unrecognized on-disk shape)
/// and is not part of the querying surface.
/// </remarks>
/// <param name="Exists">Whether the underlying conversation history file exists at the configured location.</param>
/// <param name="IsReadable">Whether the file could be opened for reading.</param>
/// <param name="IsSchemaSupported">Whether the file's internal structure matches a shape this version of the SDK understands.</param>
public sealed record MessagesDatabaseDiagnostics(bool Exists, bool IsReadable, bool IsSchemaSupported);
