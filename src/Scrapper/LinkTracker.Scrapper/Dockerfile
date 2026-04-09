FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY *.sln .
COPY src/Scrapper/LinkTracker.Scrapper/*.csproj src/Scrapper/LinkTracker.Scrapper/

RUN dotnet restore "src/Scrapper/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj"
COPY src/ src/

RUN dotnet publish "src/Scrapper/LinkTracker.Scrapper/LinkTracker.Scrapper.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8010
EXPOSE 50052

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.Scrapper.dll"]
