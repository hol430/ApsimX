FROM archlinux:base-devel

# Install git, mono, .net core sdk
RUN pacman -Sy --noconfirm git mono mono-tools dotnet-sdk

# This environment variable is required for the mono builds to work
ENV FrameworkPathOverride=/usr/lib/mono/4.7.2-api/

# Opt out of microsoft's telemetry program
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Clone apsim
RUN git clone https://github.com/APSIMInitiative/ApsimX.git /opt/ApsimX

# Build apsim
RUN dotnet build /opt/ApsimX/ApsimX.sln

WORKDIR /apsim

ENTRYPOINT ["/usr/bin/dotnet", "/opt/ApsimX/NetCoreBin/Models.dll"]