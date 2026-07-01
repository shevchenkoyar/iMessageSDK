using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Builds the SQL projection for <see cref="AttachmentRow"/> and reads rows back out of a
/// <see cref="SqliteDataReader"/>.
/// </summary>
internal static class AttachmentRowMapper
{
    /// <summary>
    /// Builds the column list (without the leading <c>SELECT</c> keyword) for projecting
    /// attachment rows. Assumes the <c>attachment</c> table is aliased <c>a</c> in the
    /// surrounding query.
    /// </summary>
    public static string BuildSelectColumns(ChatDatabaseSchema schema) => string.Join(",\n    ",
    [
        "a.ROWID AS RowId",
        "a.guid AS Guid",
        "a.filename AS FileName",
        "a.uti AS UniformTypeIdentifier",
        "a.mime_type AS MimeType",
        "a.transfer_name AS TransferName",
        "a.total_bytes AS TotalBytes",
        schema.AttachmentHasColumn("is_sticker") ? "a.is_sticker AS IsSticker" : "NULL AS IsSticker",
        schema.AttachmentHasColumn("created_date") ? "a.created_date AS CreatedDate" : "NULL AS CreatedDate",
    ]);

    public static AttachmentRow ReadRow(SqliteDataReader reader) => new()
    {
        RowId = reader.GetInt64(reader.GetOrdinal(nameof(AttachmentRow.RowId))),
        Guid = reader.GetString(reader.GetOrdinal(nameof(AttachmentRow.Guid))),
        FileName = reader.GetNullableStringColumn(nameof(AttachmentRow.FileName)),
        UniformTypeIdentifier = reader.GetNullableStringColumn(nameof(AttachmentRow.UniformTypeIdentifier)),
        MimeType = reader.GetNullableStringColumn(nameof(AttachmentRow.MimeType)),
        TransferName = reader.GetNullableStringColumn(nameof(AttachmentRow.TransferName)),
        TotalBytes = reader.GetNullableInt64Column(nameof(AttachmentRow.TotalBytes)),
        IsSticker = reader.GetBooleanColumn(nameof(AttachmentRow.IsSticker)),
        CreatedDate = reader.GetNullableInt64Column(nameof(AttachmentRow.CreatedDate)),
    };
}
