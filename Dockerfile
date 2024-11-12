ARG configuration=Development

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG configuration
WORKDIR /App

# Dependencies
COPY *.csproj ./
RUN dotnet restore

# Build
COPY . ./
RUN dotnet build --no-restore --configuration ${configuration}
RUN dotnet publish --no-build --configuration ${configuration} -o out

# Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build /App/out .

WORKDIR /srv/
EXPOSE 54970

# ENTRYPOINT ["dotnet", "SlowcialSharing.dll"]
