FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore against the project files alone so the layer survives any change that does not touch
# a dependency — the slowest step should not rerun because a controller was edited.
COPY GenomeTrack.sln ./
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY API/API.csproj API/
COPY UnitTest/UnitTest.csproj UnitTest/
RUN dotnet restore

COPY . .
RUN dotnet publish API/API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Runs as a non-root user. The base image ships one, so this costs nothing and removes the
# default of a container process owning the filesystem it runs on.
USER $APP_UID

COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "GenomeTrack.API.dll"]
