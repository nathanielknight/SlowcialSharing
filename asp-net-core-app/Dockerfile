FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App

# Dependencies
COPY *.csproj ./
RUN dotnet restore

# Build
COPY . ./
RUN dotnet build --no-restore --configuration Release
RUN dotnet publish --no-build --configuration Release -o out

# Run
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /App
COPY --from=build /App/out .

EXPOSE 54970

ENTRYPOINT ["dotnet", "SlowcialSharing.dll"]
