using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YaaraFramesGifts.Security;

namespace YaaraFramesGifts.Admin.Controllers;

[ApiController]
[Route("api/admin/images")]
[Authorize(Policy = AdminAuthenticationDefaults.Policy)]
public sealed class AdminImagesController(IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private const int MaxRequestSizeBytes = (int)(MaxImageSizeBytes + 64 * 1024);

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    public async Task<ActionResult<ImageUploadResponse>> Upload(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationError("Select an image to upload.");
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return ValidationError("Image files must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expectedContentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => null
        };

        if (expectedContentType is null
            || !string.Equals(file.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
        {
            return ValidationError("Upload a JPG, JPEG, PNG, or WEBP image.");
        }

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var read = await input.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            bytesRead += read;
        }

        if (!HasValidSignature(extension, header, bytesRead))
        {
            return ValidationError("The uploaded file does not match its image format.");
        }

        var webRoot = environment.WebRootPath
            ?? throw new InvalidOperationException("The Admin app does not have a configured web root.");
        var imageDirectory = Path.Combine(webRoot, "uploads", "catalog");
        Directory.CreateDirectory(imageDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var finalPath = Path.Combine(imageDirectory, fileName);
        var temporaryPath = Path.Combine(imageDirectory, $"{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await input.CopyToAsync(output, cancellationToken);
            }

            System.IO.File.Move(temporaryPath, finalPath);
        }
        finally
        {
            if (System.IO.File.Exists(temporaryPath))
            {
                System.IO.File.Delete(temporaryPath);
            }
        }

        return Ok(new ImageUploadResponse($"/uploads/catalog/{fileName}"));
    }

    private BadRequestObjectResult ValidationError(string message) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["file"] = [message]
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid image"
        });

    private static bool HasValidSignature(string extension, byte[] header, int bytesRead) =>
        extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8
                && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytesRead >= 12
                && header.AsSpan(0, 4).SequenceEqual("RIFF"u8)
                && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
}

public sealed record ImageUploadResponse(string ImageUrl);
