using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using GenerateThumbnail.Services;
using Microsoft.AspNetCore.Mvc;
using S3BufferedUploads;
using TeeStreaming;

namespace GenerateThumbnail.Controllers;

/// <summary>
/// Demonstration controller that generates a thumbnail, saves it to S3 and
/// returns S3 upload information.  This demonstrates how to use
/// S3BufferedUploadStream to save to S3.
/// </summary>
[ApiController]
public class GenerateThumbnailController : ControllerBase
{
    private readonly IAmazonS3 _s3Client;
    private readonly IResizeService _resizeService;
    private readonly IS3BufferedUploadStreamFactory _bufferedUploadStreamFactory;
    private readonly IGenerateThumbnailControllerOptions _options;
    private readonly ILogger<GenerateThumbnailController> _logger;

    public GenerateThumbnailController(
        IAmazonS3 s3Client,
        IResizeService resizeService,
        IS3BufferedUploadStreamFactory bufferedUploadStreamFactory,
        IGenerateThumbnailControllerOptions options,
        ILogger<GenerateThumbnailController> logger)
    {
        _s3Client = s3Client;
        _resizeService = resizeService;
        _bufferedUploadStreamFactory = bufferedUploadStreamFactory;
        _options = options;
        _logger = logger;
    }

    [HttpPost]
    [Route("/{option?}")]
    public async Task PostUsingStreams([FromRoute] string? option)
    {
        try
        {
            bool useStream;
            switch (option)
            {
                case "stream":
                case null:
                    useStream = true;
                    break;
                case "file":
                    useStream = false;
                    break;
                default:
                    throw new BadHttpRequestException("Invalid route", 400);
            }

            var (fullsizeFileName, thumbnailFileName) = GenerateFileNames(Request.Headers["Content-Type"]);
            WriteHeaders(fullsizeFileName, thumbnailFileName);
            if (useStream)
            {
                await ConversionUsingStreams(fullsizeFileName, thumbnailFileName);
                // await ConversionUsingStream(_options.S3BucketName, fullsizeFileName, thumbnailFileName);
            }
            else
            {
                await ConversionUsingTempFiles(fullsizeFileName, thumbnailFileName);
            }
        }
        catch (Exception ex)
        {
            await HandleException(ex);
        }
    }

    private Tuple<string, string> GenerateFileNames(string contentType)
    {
        string ext = "";
        if (!String.IsNullOrEmpty(contentType))
        {
            int i = contentType.LastIndexOf('/');
            if (i != -1)
            {
                ext = contentType.Substring(i + 1);
                if (!String.IsNullOrEmpty(ext))
                {
                    ext = '.' + ext;
                }
            }
        }

        var id = Guid.NewGuid();
        return new Tuple<string, string>(
            $"{id}{ext}",
            $"{id}_thumbnail.jpg"
        );
    }

    private void WriteHeaders(string fullsizeFileName, string thumbnailFileName)
    {
        Response.Headers.Add("Content-Type", "image/jpeg");
        Response.Headers.Add("Content-Disposition", "inline");
        Response.Headers.Add("X-Fullsize", String.IsNullOrEmpty(_options.S3PublicEndpoint)
            ? fullsizeFileName : _options.S3PublicEndpoint + _options.S3BucketName + '/' + fullsizeFileName);
        Response.Headers.Add("X-Thumbnail", String.IsNullOrEmpty(_options.S3PublicEndpoint)
            ? fullsizeFileName : _options.S3PublicEndpoint + _options.S3BucketName + '/' + thumbnailFileName);
    }

    private async Task HandleException(Exception exception)
    {
        _logger.LogError(exception, "Unable to process image");
        var httpException = exception as BadHttpRequestException;
        Response.StatusCode = httpException == null ? 400 : httpException.StatusCode;
        Response.Headers["Content-Type"] = "application/text";
        Response.Headers.Remove("Content-Disposition");
        Response.Headers.Remove("X-Filename");
        Response.Headers.Remove("X-Thumbnail-Filename");
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(exception.Message)).ConfigureAwait(false);
    }

    private async Task ConversionUsingStreams(string fullsizeFileName, string thumbnailFileName)
    {
        using (var s3FullSizeStream = _bufferedUploadStreamFactory.Create(_s3Client, new InitiateMultipartUploadRequest
        {
            CannedACL = S3CannedACL.PublicRead,
            BucketName = _options.S3BucketName,
            Key = fullsizeFileName
        }))
        using (var s3ThumbnailStream = _bufferedUploadStreamFactory.Create(_s3Client, new InitiateMultipartUploadRequest
        {
            CannedACL = S3CannedACL.PublicRead,
            BucketName = _options.S3BucketName,
            Key = thumbnailFileName
        }))
        using (var thumbnailStream = new TeeStream(s3ThumbnailStream, Response.Body))
        using (var inputStream = new TeeStream(TeeStream.Self, s3FullSizeStream))
        {
            var taskConversion = _resizeService.Convert(inputStream, thumbnailStream, _options.MaxThumbnailWidth, _options.MaxThumbnailHeight);
            // await inputStream.CopyFromAsync(Request.Body).ConfigureAwait(false);
            await Request.Body.CopyToAsync(inputStream);
            inputStream.SetAtEnd();
            await taskConversion.ConfigureAwait(false);
        }
    }

    private async Task ConversionUsingTempFiles(string fullsizeFileName, string thumbnailFileName)
    {
        var tempFileName = Path.GetTempFileName();
        try
        {
            using (var fsTemp = new FileStream(tempFileName, FileMode.Open))
            using (var memThumbnail = new MemoryStream(32768))
            using (var transfer = new Amazon.S3.Transfer.TransferUtility(_s3Client))
            {
                var s3Tasks = new List<ConfiguredTaskAwaitable>();

                await Request.Body.CopyToAsync(fsTemp).ConfigureAwait(false);
                fsTemp.Position = 0;
                await _resizeService.Convert(fsTemp, memThumbnail, _options.MaxThumbnailWidth, _options.MaxThumbnailHeight).ConfigureAwait(false);

                // Reset the thumbnail stream, copy to the response body
                memThumbnail.Position = 0;
                await memThumbnail.CopyToAsync(Response.Body).ConfigureAwait(false);

                // Reset the full size and thumbnail streams, upload to S3
                // We have to do S3 last because UploadAsync closes the streams
                fsTemp.Position = 0;
                memThumbnail.Position = 0;
                Task.WaitAll(
                    transfer.UploadAsync(new TransferUtilityUploadRequest
                    {
                        InputStream = fsTemp,
                        CannedACL = S3CannedACL.PublicRead,
                        BucketName = _options.S3BucketName,
                        Key = fullsizeFileName
                    }),
                    transfer.UploadAsync(new TransferUtilityUploadRequest
                    {
                        InputStream = memThumbnail,
                        CannedACL = S3CannedACL.PublicRead,
                        BucketName = _options.S3BucketName,
                        Key = thumbnailFileName
                    })
                );
            }
        }
        finally
        {
            System.IO.File.Delete(tempFileName);
        }
    }
}

public interface IGenerateThumbnailControllerOptions
{
    string S3BucketName { get; }
    int MaxThumbnailWidth { get; }
    int MaxThumbnailHeight { get; }
    string? S3PublicEndpoint { get; }
}

public class GenerateThumbnailControllerOptions : IGenerateThumbnailControllerOptions
{
    public GenerateThumbnailControllerOptions(string s3BucketName, int maxThumbnailWidth, int maxThumbnailHeight, string? s3PublicEndpoint)
    {
        S3BucketName = s3BucketName;
        MaxThumbnailWidth = maxThumbnailWidth;
        MaxThumbnailHeight = maxThumbnailHeight;
        S3PublicEndpoint = s3PublicEndpoint;
    }
    public string S3BucketName { get; }
    public int MaxThumbnailWidth { get; }
    public int MaxThumbnailHeight { get; }
    public string? S3PublicEndpoint { get; }
}