# S3BufferedUpload and TeeStreaming Demonstration

This is a demonstration application that can be run either as an ASP.NET Core webservice or Console application.  It demonstrates using the **S3BufferedUploadStream** and **TeeStream** classes available at:

| Library          | GitHub                                           | NuGet                                            |
|------------------|--------------------------------------------------|--------------------------------------------------|
| TeeStreaming     | https://github.com/jasonterando/TeeStreaming     | https://www.nuget.org/packages/TeeStreaming/     |
| S3BufferedUpload | https://github.com/jasonterando/S3BufferedUpload | https://www.nuget.org/packages/S3BufferedUpload/ |

These libraries facilitate writing to more than one stream simultaneously and writing to S3 using a stream, respectively.

The web service demo saves both a fullsize and thumbnail image of a posted image to S3, and return the thumbnail image as the response payload.  Response headers include the URLs to the saved S3 images.

The console demo will convert the input image (URL or file system) to a thumbnail, and save to one or more output destinations (S3 or file system).

## Requirements

This demonstration requires [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and [ImageMagick](https://imagemagick.org/script/download.php) to run.  If you do not have these and do not want to download them, there is a Docker implementation included in the project.  The Docker implementation uses LocalStack to simulate AWS S3 locally.

If you use [Visual Studio Code](https://duckduckgo.com/?t=lm&q=visual+studio+code+download&ia=web), you can launch the server and console via debug tasks.

## Running the Demonstration

### LocalStack / S3

The demo requires access to either AWS S3 or LocalStack.  If you are running this demo without Docker but want to mock S3 using LocalStack using the included Docker configuration, navigate to the project directory and run `docker-compose up localstack` to launch LocalStack S3 services.  S3 commands are persisted in `.aws/data/*.json` files, which you can delete when you want to remove test S3 objects.

### Execute Webservice Demo Via Local .NET Core

From a terminal window, navigate to the project's `/src` folder and run `dotnet run server --s3-bucket-name=test` if connecting to S3, or `dotnet run server --s3-service-url=http://localhost:4566 --s3-bucket-name=test` if connecting to LocalStack.  You can also debug the application from Visual Studio Code by launching the debug configuration "Local ASP.NET Core Demo".

Once running, post an image to `http://localhost:5000` or `https://localhost:5001` (don't forget to run `dotnet dev-certs https` if you haven't already).  You should get back an image with headers "X-Fullsize" indicating the S3 full-size URL and "X-Thumbnail" indicating the S3 thumbnail URL.

### Execute Webservice Demo Via Docker

You can run the webservice using the Docker compose file in the project directory using `docker-compose up`. Once the Docker Compose stack is running and fully initialized, usage is the same as above.

### Webservice command line arguments:

| Argument               | Description                                            |
|------------------------|--------------------------------------------------------|
| server                 | Required. Launches demo in server (webservice) mode    |
| --launch-profile       | ASP.NET Core launch profile                            |
| --s3-bucket-name       | Required. S3 Bucket Name                               |
| --s3-service-url       | (Default: None) S3 API endpoint                        |
| --max-thumbnail-width  | (Default: 200) Maximum thumbnail width                 |
| --max-thumbnail-height | (Default: 200) Maximum thumbnail height                |
| --convert-path         | (Default: convert) Path to ImageMagick convert utility |

> Note: If you are running the service in Windows, you will probably have to set **--convert-path** to the fully qualified location of the ImageMagick Convert utility because of name collisions with other applications called **convert**

### Execute Console Demo via Local .NET Core

From a terminal window, navigate to the project's `/src` folder and run `dotnet run console --s3-bucket-name=test` if connecting to S3, or `dotnet run server --s3-service-url=http://localhost:4572 --s3-bucket-name=test` if connecting to LocalStack.  You can also debug the application from Visual Studio Code by launching the debug configuration "Local ASP.NET Core Demo".

### Execute Console Demo via Docker

You can run the console demo using the Docker compose file in the project directory using `docker-compose run app --s3-service-url=http://localstack:4566 sample1.jpg sample1-thumbnail.jpg s3://test/sample1-thumbnail.jpg`.  For this to work, the following needs to happen:

1. You must have `docker-compose run localstack` started and initialized
2. If you want to test other files, copy them to the `samples` directory, which is mounted to the Docker container


### Console command line arguments

| Argument               | Description                                            |
|------------------------|--------------------------------------------------------|
| console                | (Optional, Default). Launches demo in console mode     |
| --launch-profile       | ASP.NET Core launch profile                            |
| --s3-service-url       | (Default: None) S3 API endpoint                        |
| --max-thumbnail-width  | (Default: 200) Maximum thumbnail width                 |
| --max-thumbnail-height | (Default: 200) Maximum thumbnail height                |
| --convert-path         | (Default: convert) Path to ImageMagick convert utility |
| First Value            | Path or HTTP/HTTPS URL for image to convert            |
|                        |   (specify -- or stdin for STDIN)                      |
| Second..Nth Values     | HTTP/HTTPS URL or S3 URL to save image to              |
|                        |   (specify -- or stdout for STDOUT                      |
