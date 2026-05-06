namespace FlashFortune.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>Uploads a file stream and returns the storage key (S3 object key).</summary>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Returns a pre-signed URL valid for download (for auditors).</summary>
    Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default);
}
