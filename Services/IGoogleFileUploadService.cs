namespace WfdHrms.Services;

public record FileUploadResult(
    bool Success,
    string? FileId,
    string? FileUrl,
    long FileSizeBytes,
    string? ErrorMessage = null
);

public interface IGoogleFileUploadService
{
    Task<FileUploadResult> UploadFileAsync(
        Stream stream,
        string fileName,
        string mimeType,
        string documentType,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);
}
