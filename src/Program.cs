using System;
using CommandLine;
using GenerateThumbnail.Models;
using GenerateThumbnail.Services;

namespace GenerateThumbnail;

public class GenerateThumbnail
{
    /// <summary>
    /// Dispatched parsed command line arguments to server or console service
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var result = Parser.Default.ParseArguments<ServerOptions, ConsoleOptions>(args);
            await result.WithParsedAsync<ServerOptions>(options => ServerService.Run(options, args));
            await result.WithParsedAsync<ConsoleOptions>(options => ConsoleService.Run(options));
            return 0;
        } 
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex);
            return -1;
        }
    }
}
