using System.Text.RegularExpressions;
using Amazon.S3;
using S3BufferedUploads;
using TeeStreaming;
using GenerateThumbnail.Models;

namespace GenerateThumbnail.Services;

/// <summary>
/// This class 
/// </summary>
public class ConsoleService
{
    private readonly static Regex _regexS3 = new Regex(@"s3:\/\/((?!xn--)(?!.*-s3alias)[a-z0-9][a-z0-9-]{1,61}[a-z0-9])\/(.*)");
    private readonly static Regex _regexWebURL = new Regex(@"^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&\%\$#_]*)?$");

    public static async Task Run(ConsoleOptions options)
    {
        Stream? input = null;
        List<Stream> outputs = new List<Stream>(options.Output.Count());

        AmazonS3Client? s3Client = null;

        // Set up input

        if (_regexS3.IsMatch(options.Input))
        {
            throw new ArgumentException("Input cannot be an S3 URL");
        }
        else if (options.Input.ToLower() == "stdin" || options.Input == "-")
        {
            Console.Error.WriteLine("Input from STDIO");
            input = System.Console.OpenStandardInput();
        }
        else if (_regexWebURL.IsMatch(options.Input))
        {
            Console.Error.WriteLine($"Input from Web URL \"{options.Input}\"");
            input = await (new HttpClient()).GetStreamAsync(options.Input);
        }
        else
        {
            Console.Error.WriteLine($"Input from File \"{options.Input}\"");
            input = new FileStream(options.Input, FileMode.Open, FileAccess.Read);
        }

        // Set up outputs

        foreach (var optionOutput in options.Output ?? new string[] { })
        {
            if (_regexWebURL.IsMatch(optionOutput))
            {
                throw new ArgumentException("Output cannot be a Web URL");
            }
            else if (optionOutput.ToLower() == "stdout" || optionOutput == "-")
            {
                Console.Error.WriteLine($"Output to STDOUT");
                outputs.Add(System.Console.OpenStandardOutput());
            }
            else if (_regexS3.IsMatch(optionOutput))
            {
                var groups = _regexS3.Match(optionOutput).Groups;
                var bucketName = groups[1]?.Value ?? throw new ArgumentException("Unable to get bucket name from S3 URL");
                var key = groups[2]?.Value ?? throw new ArgumentException("Unable to get key from S3 URL");
                if (s3Client == null)
                {
                    var config = new AmazonS3Config();
                    if (! string.IsNullOrEmpty(options.S3ServiceURL)) {
                        Console.Error.WriteLine($"Setting S3 endpoint to \"{options.S3ServiceURL}\"");
                        config.ServiceURL = options.S3ServiceURL;
                        config.UseHttp = options.S3ServiceURL.StartsWith("http:");
                        config.ForcePathStyle = true;
                    }

                    s3Client = new AmazonS3Client(config);
                }
                Console.Error.WriteLine($"Output to S3 bucket \"{bucketName}\", key \"{key}\"");
                outputs.Add(new S3BufferedUploadStream(s3Client, bucketName, key));
            }
            else
            {
                Console.Error.WriteLine($"Output to file \"{optionOutput}\"");
                outputs.Add(new FileStream(optionOutput, FileMode.Create));
            }
        }

        if (input == null)
        {
            throw new Exception("A input is required");
        }

        if (outputs.Count == 0)
        {
            throw new Exception("At least one output is required");
        }

        // Process

        var outputToProcess = outputs.Count == 1 ? outputs[0] : new TeeStream(outputs.ToArray());
        await (new ResizeService("convert")).Convert(input, outputToProcess, options.MaxThumbnailWidth, options.MaxHumbnailHeight);
        outputToProcess.Close();
    }
}