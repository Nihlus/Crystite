# First stage - Build Crystite from source
FROM public.ecr.aws/docker/library/debian:12

USER root
WORKDIR /root

RUN apt-get update
RUN apt-get install -y wget git
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y dotnet-sdk-8.0
COPY . /root/Crystite
WORKDIR /root/Crystite
RUN dotnet publish -f net8.0 -c Release -r linux-x64 --self-contained false -o bin/crystite Crystite/Crystite.csproj
RUN dotnet publish -f net8.0 -c Release -r linux-x64 --self-contained false -o bin/crystitectl Crystite.Control/Crystite.Control.csproj

# Second stage - Copy build artifacts and configure application container
FROM public.ecr.aws/docker/library/debian:12-slim

USER root
WORKDIR /root

COPY --from=0 /root/Crystite/bin/crystite /usr/lib/crystite
COPY --from=0 /root/Crystite/bin/crystitectl /usr/lib/crystitectl

RUN apt-get update
RUN apt-get install -y wget
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0 libassimp5 libfreeimage3 libfreetype6 libopus0 libbrotli1 zlib1g yt-dlp jq
RUN mkdir /Config /Data /Cache /Logs /var/lib/crystite /var/lib/crystite/Resonite
VOLUME [ "/Data", "/Logs", "/var/lib/crystite/Resonite" ]
COPY docker/appsettings.json /etc/crystite/appsettings.json
COPY docker/Config.json /Config/Config.json
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
WORKDIR /var/lib/crystite

ENTRYPOINT [ "sh", "/entrypoint.sh" ]
CMD ["/usr/lib/crystite/crystite"]