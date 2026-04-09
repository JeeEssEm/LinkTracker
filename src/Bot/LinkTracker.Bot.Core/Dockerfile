FROM mcr.microsoft.com/dotnet/sdk:9.0
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .
RUN dotnet restore "src/Bot/LinkTracker.Bot.Core/LinkTracker.Bot.Core.csproj"
RUN dotnet build "src/Bot/LinkTracker.Bot.Core/LinkTracker.Bot.Core.csproj" -c $BUILD_CONFIGURATION --no-restore

ENTRYPOINT ["dotnet", "build", "src/Bot/LinkTracker.Bot.Core/LinkTracker.Bot.Core.csproj", "-c", "Release", "--no-restore"]
