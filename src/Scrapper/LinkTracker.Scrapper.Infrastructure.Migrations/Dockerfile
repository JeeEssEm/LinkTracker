FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY *.sln .
COPY src/Scrapper/LinkTracker.Scrapper.Infrastructure.Migrations/*.csproj src/Scrapper/LinkTracker.Scrapper.Infrastructure.Migrations/

RUN dotnet restore "src/Scrapper/LinkTracker.Scrapper.Infrastructure.Migrations/LinkTracker.Scrapper.Infrastructure.Migrations.csproj"
COPY src/ src/

RUN dotnet publish "src/Scrapper/LinkTracker.Scrapper.Infrastructure.Migrations/LinkTracker.Scrapper.Infrastructure.Migrations.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.Scrapper.Infrastructure.Migrations.dll"]
