namespace WfdHrms.Services;

public class GoogleFileUploadService : IGoogleFileUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleFileUploadService> _logger;

    public GoogleFileUploadService(
        IWebHostEnvironment env,
        IConfiguration config,
        ILogger<GoogleFileUploadService> logger)
    {
        _env = env;
        _config = config;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadFileAsync(
        Stream stream,
        string fileName,
        string mimeType,
        string documentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var maxBytes = _config.GetValue<long>("UploadSettings:MaxFileSizeBytes", 2097152);
            var allowedMimes = _config.GetSection("UploadSettings:AllowedMimeTypes").Get<string[]>() ?? ["image/webp"];

            if (!allowedMimes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
            {
                return new FileUploadResult(false, null, null, 0, $"Invalid MIME type '{mimeType}'. Only {string.Join(", ", allowedMimes)} are allowed.");
            }

            if (stream.Length > maxBytes)
            {
                return new FileUploadResult(false, null, null, 0, $"File size ({stream.Length} bytes) exceeds limit of {maxBytes} bytes (2 MB).");
            }

            // Local simulation storage for reliable testing
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", documentType.ToLowerInvariant());
            Directory.CreateDirectory(uploadsDir);

            var safeFileId = $"gdrive_sim_{Guid.NewGuid():N}";
            var fileExtension = ".webp";
            var diskFileName = $"{safeFileId}{fileExtension}";
            var filePath = Path.Combine(uploadsDir, diskFileName);

            await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var relativeUrl = $"/uploads/{documentType.ToLowerInvariant()}/{diskFileName}";
            var length = new FileInfo(filePath).Length;

            _logger.LogInformation("File uploaded successfully. Document: {Type}, FileId: {FileId}, Path: {Path}", documentType, safeFileId, relativeUrl);

            return new FileUploadResult(true, safeFileId, relativeUrl, length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {Type}", documentType);
            return new FileUploadResult(false, null, null, 0, $"Upload failed: {ex.Message}");
        }
    }

    public Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var baseUploads = Path.Combine(_env.WebRootPath, "uploads");
            if (Directory.Exists(baseUploads))
            {
                var files = Directory.GetFiles(baseUploads, $"{fileId}.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    File.Delete(file);
                    _logger.LogInformation("Deleted file {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {FileId}", fileId);
        }

        return Task.CompletedTask;
    }
}
