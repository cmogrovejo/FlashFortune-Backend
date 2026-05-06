using Amazon.S3;
using Amazon.S3.Model;
using FlashFortune.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlashFortune.Infrastructure.Storage;

public sealed class S3FileStorageService(IAmazonS3 s3, IConfiguration config) : IFileStorageService
{
    private string BucketName => config["Storage:BucketName"]!;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var key = $"uploads/{Guid.NewGuid()}/{fileName}";

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // Object Lock enforced via bucket policy (WORM)
        }, ct);

        return key;
    }

    public async Task<string> GetDownloadUrlAsync(string fileKey, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = fileKey,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        return await Task.FromResult(s3.GetPreSignedURL(request));
    }
}
