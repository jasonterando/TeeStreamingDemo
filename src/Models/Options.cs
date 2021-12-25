using CommandLine;

namespace GenerateThumbnail.Models;

public class BaseOptions
{
    [Option("s3-service-url", Required = false, Default = null, HelpText = "S3 API endpoint")]
    public string? S3ServiceURL { get; set; } = null;

    // [Option("aws-anonymous-creds", Required = false, Default = false, HelpText = "Set to true to use anonymous credentials with AWS")]
    // public bool UseAWSAnonymousCredentials { get; set; } = false;

    [Option("max-thumbnail-width", Default = 200, HelpText = "Maximum thumbnail width")]
    public int MaxThumbnailWidth { get; set; } = 200;

    [Option("max-thumbnail-height", Default = 200, HelpText = "Maximum thumbnail height")]
    public int MaxHumbnailHeight { get; set; } = 200;

    [Option("convert-path", Required = false, Default = "convert", HelpText = "Path to ImageMagick convert utility")]
    public string ConvertPath { get; set; } = "convert";
}

[Verb("server", isDefault: false, HelpText = "Launch web service server for conversion")]
public class ServerOptions: BaseOptions
{
    [Option("launch-profile", Required = false, Default = null, HelpText = "ASP.NET Core launch profile")]
    public string? LaunchProfile { get; set; } = null;

    [Option("s3-bucket-name", Required = true, HelpText = "S3 Bucket Name")]
    public string S3BucketName { get; set; } = "";
}

[Verb("console", isDefault: true, HelpText = "Execute conversion as a console command")]
public class ConsoleOptions: BaseOptions
{

    [Value(0, Required = true, HelpText = "Input (File Name, Web URL, \"stdin\" or \"-\" for STDIN)")]
    public string Input { get; set; } = "";

    [Value(1, Required = true, HelpText = "Output (File Name, S3 URL, \"stdout\" or \"-\" for STDOUT)", Min = 1, Max = 10)]
    public IEnumerable<string> Output { get; set; } = new string[] { };
}

