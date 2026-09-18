FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /App

# Copy everything
COPY . ./
# Restore and publish a release build
RUN cd /App/RemoTermAPI && \
    dotnet restore && \
    dotnet build -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /App
COPY --from=build /App/RemoTermAPI/out .
ENTRYPOINT ["dotnet", "RemoTermAPI.dll"]