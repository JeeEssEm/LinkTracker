FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY *.sln .
COPY src/AiAgentService/AiAgentService.Web/*.csproj src/AiAgentService/AiAgentService.Web

RUN dotnet restore "src/AiAgentService/AiAgentService.Web/AiAgentService.Web.csproj"
COPY src/ src/

RUN dotnet publish "src/AiAgentService/AiAgentService.Web/AiAgentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8020

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AiAgentService.Web.dll"]
