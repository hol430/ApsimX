FROM archlinux:base-devel

# Install git, mono, .net core sdk, and R
RUN pacman -Syu --noconfirm git mono mono-tools dotnet-sdk r gcc-fortran

# Install nunit3-console from AUR
RUN git clone https://aur.archlinux.org/nunit3-console; \
    cd nunit3-console; \
    makepkg -si --noconfirm

# Install R package dependencies
RUN R -q -e 'install.packages(c("remotes", "sensitivity", "RSQLite", "DBI"), repos = "https://cran.csiro.au/")'
RUN R -q -e 'remotes::install_github("hol430/ApsimOnR", INSTALL_opts = c("--no-docs", "--no-help", "--no-html"), repos = "https://cran.csiro.au/")'
RUN R -q -e 'remotes::install_github("SticsRPacks/CroptimizR", INSTALL_opts = c("--no-docs", "--no-help", "--no-html"), repos = "https://cran.csiro.au/")'

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
