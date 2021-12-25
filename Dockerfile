FROM mcr.microsoft.com/dotnet/sdk:6.0

EXPOSE 5000
EXPOSE 5001
EXPOSE 28251
EXPOSE 44320
RUN dotnet dev-certs https \
    && apt-get update \
    && apt-get install -y imagemagick ghostscript
 
ADD /src /app

RUN cd /app && \
    dotnet publish --output /usr/local/bin --self-contained true --runtime linux-x64 -p:PublishSingleFile=true -p:PublishTrimmed=True -p:TrimMode=Link

RUN sed -i '/disable ghostscript format types/,+6d' /etc/ImageMagick-6/policy.xml

# ENTRYPOINT [ "dotnet", "run", "--project", "/app/GenerateThumbnail.csproj" ]
ENTRYPOINT [ "/usr/local/bin/GenerateThumbnail" ]