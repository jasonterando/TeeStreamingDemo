namespace GenerateThumbnail.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class ResizeService: IResizeService
{
    private string _convertPath;

    public ResizeService(string convertPath)
    {
        _convertPath = convertPath;
    }

    /// <summary>
    /// Call Imagemagick Convert from the command line, piping in the image
    /// via STDIN and getting results from STDOUT.  If anything goes
    /// wrong, throw an exception with the contents of STDERR.
    /// 
    /// Why not use Magick.Net?  Because it does not handle streams
    /// very well, at all.  It pretty much relies upon temp files.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="maxWidth"></param>
    /// <param name="maxHeight"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>/
    public async Task Convert(Stream input, Stream output, int? maxWidth, int? maxHeight)
    {
        var info = new ProcessStartInfo
        {
            FileName = _convertPath,
            Arguments = $"-resize \"{(maxWidth == null ? "" : maxWidth.ToString())}x{(maxHeight == null ? "" : maxHeight.ToString())}\" - jpg:-",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var process = Process.Start(info);
        if (process == null) {
            throw new Exception($"Unable to start \"{_convertPath}\"");
        }

        await input.CopyToAsync(process.StandardInput.BaseStream);
        process.StandardInput.Close();
        await process.StandardOutput.BaseStream.CopyToAsync(output);
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0) {
            throw new Exception(stderr.Length > 0 ? stderr : $"Error converting image, exit code: {process.ExitCode}");
        }
    }
}

public interface IResizeService
{
    Task Convert(Stream input, Stream output, int? maxWidth, int? maxHeight);
}