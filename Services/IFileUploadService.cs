namespace WfdHrms.Services;

public interface IFileUploadService
{
    Task<FileUploadResult> UploadFileAsync(
        Stream stream,
        string originalFileName,
        string documentType,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
