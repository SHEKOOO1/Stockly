using Stockly.Domain.Common;

namespace Stockly.Domain.Modules.Files;

/// <summary>
/// files.Attachment (§4.9). UQ: <c>StorageKey</c> <strong>globally</strong> — the
/// ADR-0032 exception: a server-generated GUID path must resolve to exactly one
/// blob, because <c>Append</c>/<c>GET</c>/<c>DELETE</c> by key have no tenant
/// context. CHECK: <c>SizeBytes &gt;= 0</c>.
/// <para>
/// The original file name is <strong>metadata only</strong> and is never used to
/// build a path. <c>ExtractedText</c> is reserved for V2 OCR, so V1 never calls an
/// OCR engine.
/// </para>
/// </summary>
public class Attachment : TenantScopedSoftDeleteEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>binary(32). Lets an integrity test prove stored bytes were not altered.</summary>
    public byte[] Sha256 { get; set; } = [];

    public string StorageKey { get; set; } = string.Empty;

    public Guid UploadedByMembershipId { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    public OcrStatus OcrStatus { get; set; } = OcrStatus.NotApplicable;

    /// <summary>Reserved for V2 OCR (TBD-27 / ADR-0042). Never written in V1.</summary>
    public string? ExtractedText { get; set; }
}