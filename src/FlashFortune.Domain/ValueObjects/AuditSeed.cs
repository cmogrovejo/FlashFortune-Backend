namespace FlashFortune.Domain.ValueObjects;

/// <summary>
/// Cryptographic seed that binds the draw result to the uploaded file.
/// Seed = CSPRNG value XOR'd with SHA-256(fileContent).
/// This proves that no manual intervention occurred after the file was locked.
/// </summary>
public sealed record AuditSeed(string RandomComponent, string FileHash)
{
    public string Combined => $"{RandomComponent}:{FileHash}";

    public override string ToString() => Combined;
}
