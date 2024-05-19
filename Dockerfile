FROM public.ecr.aws/docker/library/debian:12

USER root
WORKDIR /root

RUN apt-get update
RUN apt-get install -y wget git
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y dotnet-sdk-7.0 dotnet-sdk-8.0
COPY . /root/Crystite
WORKDIR /root/Crystite
# RUN git checkout $(git tag -l --sort=-taggerdate | head -n1)
RUN dotnet publish -f net7.0 -c Release -r linux-x64 --self-contained false -o bin/crystite Crystite/Crystite.csproj
RUN dotnet publish -f net8.0 -c Release -r linux-x64 --self-contained false -o bin/crystitectl Crystite.Control/Crystite.Control.csproj
# RUN cp -Rp ./bin/crystite /usr/lib/crystite
# RUN cp -Rp ./bin/crystitectl /usr/lib/crystitectl

# Second stage of Dockerfile
FROM public.ecr.aws/docker/library/debian:12

#ENV \
#    LANG="en_US.UTF-8" \
#    LC_ALL="en_US.UTF-8"
USER root
WORKDIR /root

COPY --from=0 /root/Crystite/bin/crystite /usr/lib/crystite
COPY --from=0 /root/Crystite/bin/crystitectl /usr/lib/crystitectl

RUN apt-get update
RUN apt-get install -y wget
RUN wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb
RUN echo "deb http://ftp.us.debian.org/debian bookworm main non-free" | tee /etc/apt/sources.list.d/non-free.list
RUN dpkg --add-architecture i386
# Automatically agree to steamcmd EULA prompt during install
RUN echo steam steam/question select "I AGREE" | debconf-set-selections
RUN echo steam steam/license note '' | debconf-set-selections
RUN apt-get update
RUN apt-get install -y dotnet-runtime-7.0 dotnet-runtime-8.0 aspnetcore-runtime-7.0 aspnetcore-runtime-8.0 libassimp5 libfreeimage3 libfreetype6 libopus0 libbrotli1 zlib1g youtube-dl steamcmd jq
# RUN mkdir /var/lib/crystite/.steam/
# RUN ln -s /var/lib/crystite/.local/share/Steam/steamcmd/linux64 /var/lib/crystite/.steam/sdk64
RUN mkdir /Config /Logs
COPY docker/appsettings.json /etc/crystite/appsettings.json
COPY docker/Config.json /Config/Config.json
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
WORKDIR /var/lib/crystite

ENTRYPOINT [ "sh", "/entrypoint.sh" ]
CMD ["/usr/lib/crystite/crystite"]