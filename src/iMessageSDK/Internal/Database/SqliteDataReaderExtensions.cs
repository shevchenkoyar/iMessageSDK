using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Null-tolerant column accessors shared by the row mappers, since chat.db columns are frequently
/// <c>NULL</c> even when logically "present" on the current schema.
/// </summary>
internal static class SqliteDataReaderExtensions
{
    public static bool GetBooleanColumn(this SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return !reader.IsDBNull(ordinal) && reader.GetInt64(ordinal) != 0;
    }

    public static string? GetNullableStringColumn(this SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static long? GetNullableInt64Column(this SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    public static byte[]? GetNullableBytesColumn(this SqliteDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : (byte[])reader.GetValue(ordinal);
    }
}
