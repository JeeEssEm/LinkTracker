FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY *.sln .
COPY src/Bot/LinkTracker.Bot.App/*.csproj src/Bot/LinkTracker.Bot.App/

RUN dotnet restore "src/Bot/LinkTracker.Bot.App/LinkTracker.Bot.App.csproj"
COPY src/ src/

RUN dotnet publish "src/Bot/LinkTracker.Bot.App/LinkTracker.Bot.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8009
EXPOSE 50051

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LinkTracker.Bot.App.dll"]
