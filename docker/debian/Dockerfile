FROM debian:latest

RUN apt update && apt install -y apt-transport-https dirmngr gnupg ca-certificates wget

# Install latest mono from their repo
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF; \
echo "deb https://download.mono-project.com/repo/debian stable-buster main" | tee /etc/apt/sources.list.d/mono-official-stable.list; \
apt-key adv --keyserver keys.gnupg.net --recv-key 'E19F5F87128899B192B1A2C2AD5F960A256A04AF'; \
echo "deb http://cloud.r-project.org/bin/linux/debian buster-cran40/" | tee /etc/apt/sources.list.d/r-cran40.list

# Install microsoft's signing key
RUN wget https://packages.microsoft.com/config/debian/10/packages-microsoft-prod.deb -O packages-microsoft-prod.deb; \
dpkg -i packages-microsoft-prod.deb; \
rm packages-microsoft-prod.deb

# Update package repos
RUN apt update

# Install compilation dependencies
RUN apt install -y git gtk-sharp2 sqlite3 mono-devel dotnet-sdk-3.1 openjdk-11-jre r-base r-base-dev

# Install R package dependencies
RUN R -q -e 'install.packages(c("remotes", "sensitivity", "RSQLite", "DBI"), repos = "https://cran.csiro.au/")'
RUN R -q -e 'remotes::install_github("hol430/ApsimOnR", INSTALL_opts = c("--no-docs", "--no-help", "--no-html"), repos = "https://cran.csiro.au/")'
RUN R -q -e 'remotes::install_github("SticsRPacks/CroptimizR", INSTALL_opts = c("--no-docs", "--no-help", "--no-html"), repos = "https://cran.csiro.au/")'

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
