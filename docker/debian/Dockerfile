FROM debian:latest

RUN apt update && apt install -y apt-transport-https dirmngr gnupg ca-certificates wget

# Install latest mono from their repo
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF; \
echo "deb https://download.mono-project.com/repo/debian stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list

# Install microsoft's signing key
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb; \
dpkg -i packages-microsoft-prod.deb; \
rm packages-microsoft-prod.deb

# Update package repos
RUN apt update

# Install compilation dependencies
RUN apt install -y git gtk-sharp2 sqlite3 mono-devel dotnet-sdk-3.1 openjdk-11-jre

# This environment variable is required for the mono builds to work
ENV FrameworkPathOverride=/usr/lib/mono/4.7.2-api/

# Opt out of microsoft's telemetry program
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# This is a temporary hack to assist in loading libsqlite in .net core builds
RUN ln -s /usr/lib/x86_64-linux-gnu/libsqlite3.so.0 /usr/lib/x86_64-linux-gnu/libsqlite3.so

# Clone apsim
RUN git clone https://github.com/APSIMInitiative/ApsimX.git /opt/ApsimX

# Build apsim
RUN dotnet build /opt/ApsimX/ApsimX.sln

WORKDIR /apsim

ENTRYPOINT ["/usr/bin/dotnet", "/opt/ApsimX/NetCoreBin/Models.dll"]
