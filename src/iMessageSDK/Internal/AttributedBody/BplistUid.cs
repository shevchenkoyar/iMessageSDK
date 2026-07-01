namespace iMessageSDK.Internal.AttributedBody;

/// <summary>
/// A binary property list "UID" value: an object reference used by NSKeyedArchiver to point at
/// another entry in the archive's flat <c>$objects</c> array.
/// </summary>
internal readonly record struct BplistUid(int Index);
