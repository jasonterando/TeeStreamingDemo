using Amazon.Runtime;
using Amazon.S3;
using GenerateThumbnail.Controllers;
using GenerateThumbnail.Models;
using S3BufferedUploads;

namespace GenerateThumbnail.Services;

public class ServerService
{
    public static Task Run(ServerOptions options, string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var settings = builder.Configuration.GetSection("Settings");

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IS3BufferedUploadStreamFactory>(S3BufferedUploadStreamFactory.Default);
        builder.Services.AddSingleton<IGenerateThumbnailControllerOptions>(provider =>
        {
            if (options.S3ServiceURL?.Length > 0 && options.S3ServiceURL[options.S3ServiceURL.Length - 1] != '/')
            {
                options.S3ServiceURL+= '/';
            }
            return new GenerateThumbnailControllerOptions(
                options.S3BucketName,
                options.MaxThumbnailWidth,
                options.MaxHumbnailHeight,
                options.S3ServiceURL
        );
        });
        builder.Services.AddSingleton<IResizeService>(provider =>
            new ResizeService(
                options.ConvertPath
            )
        );
        builder.Services.AddSingleton<IAmazonS3>(provider =>
        {
            AmazonS3Config? config = null;
            if (!string.IsNullOrEmpty(options.S3ServiceURL))
            {
                var endpoint = options.S3ServiceURL;
                System.Console.WriteLine($"S3Endpoint = {endpoint}");
                if (!String.IsNullOrEmpty(endpoint))
                {
                    config = new AmazonS3Config
                    {
                        UseHttp = endpoint.StartsWith("http:"),
                        ServiceURL = endpoint,
                        ForcePathStyle = true
                    };
                }
            }
            
            return new AmazonS3Client(config);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // app.Use((context, next) =>
        // {
        //     context.Request.EnableBuffering();
        //     return next();
        // });

        return app.RunAsync();
    }
}