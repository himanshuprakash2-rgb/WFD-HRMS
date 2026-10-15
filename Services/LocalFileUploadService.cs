namespace WfdHrms.Services;

public class LocalFileUploadService : IFileUploadService, IGoogleFileUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileUploadService> _logger;

    public LocalFileUploadService(
        IWebHostEnvironment env,
        ILogger<LocalFileUploadService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadFileAsync(
        Stream stream,
        string originalFileName,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var subFolder = documentType.Trim().ToLowerInvariant().Replace(" ", "_");
            var uploadsDir = Path.Combine(webRoot, "uploads", subFolder);
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(originalFileName);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".png";
            }

            var safeFileId = $"{subFolder}_{Guid.NewGuid():N}{ext}";
            var destinationPath = Path.Combine(uploadsDir, safeFileId);

            await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var length = new FileInfo(destinationPath).Length;
            var relativeUrl = $"/uploads/{subFolder}/{safeFileId}";

            _logger.LogInformation("File uploaded to wwwroot: {Path} ({Bytes} bytes)", relativeUrl, length);

            return new FileUploadResult(true, safeFileId, relativeUrl, length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file for document type {Type}", documentType);
            return new FileUploadResult(false, null, null, 0, $"File upload failed: {ex.Message}");
        }
    }

    // Implementation for IGoogleFileUploadService backward-compatibility
    public Task<FileUploadResult> UploadFileAsync(
        Stream stream,
        string fileName,
        string mimeType,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        return UploadFileAsync(stream, fileName, documentType, cancellationToken);
    }

    public Task DeleteFileAsync(string fileUrlOrId, CancellationToken cancellationToken = default)
    {
        try
        {
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var cleanPath = fileUrlOrId.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRoot, cleanPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted file: {Path}", fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {File}", fileUrlOrId);
        }

        return Task.CompletedTask;
    }
}
