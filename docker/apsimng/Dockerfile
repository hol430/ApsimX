# APSIM Next Generation
#
# To build this container:
#    dotnet build ApsimX.sln && docker build -t apsiminitiative/apsimng docker/run
# To run the container:
#    docker run -i --rm -v "$PWD:/apsim" apsiminitiative/apsimng path/to/my/file.apsimx

FROM mcr.microsoft.com/dotnet/core/runtime

# Install some packages
RUN apt update
RUN apt install -y sqlite3

# Copy prebuilt assemblies into the container
COPY ../../NetCoreBin/* /opt/ApsimX/

# Unpack APSIM Next Generation
RUN tar xf $APSIM_INSTALL

# Clean up
RUN rm $APSIM_INSTALL

# Run APSIM
ENTRYPOINT ["dotnet", "/opt/ApsimX/Models.dll"]