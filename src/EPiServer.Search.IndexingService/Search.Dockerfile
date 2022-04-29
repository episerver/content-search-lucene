FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
WORKDIR /app
EXPOSE 8000

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["src/EPiServer.Search.IndexingService/EPiServer.Search.IndexingService.csproj", "EPiServer.Search.IndexingService/"]
RUN dotnet restore "EPiServer.Search.IndexingService/EPiServer.Search.IndexingService.csproj"

COPY ["src/EPiServer.Search.IndexingService/", "EPiServer.Search.IndexingService/"]
RUN dotnet build "EPiServer.Search.IndexingService/EPiServer.Search.IndexingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EPiServer.Search.IndexingService/EPiServer.Search.IndexingService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EPiServer.Search.IndexingService.dll"]
