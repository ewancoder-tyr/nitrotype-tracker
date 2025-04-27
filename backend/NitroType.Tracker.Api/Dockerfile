FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["NitroType.Tracker.Api/NitroType.Tracker.Api.csproj", "NitroType.Tracker.Api/"]
COPY ["NitroType.Tracker.Domain/NitroType.Tracker.Domain.csproj", "NitroType.Tracker.Domain/"]
RUN dotnet restore "NitroType.Tracker.Api/NitroType.Tracker.Api.csproj"
COPY . .
WORKDIR "/src/NitroType.Tracker.Api"
RUN dotnet build "NitroType.Tracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "NitroType.Tracker.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NitroType.Tracker.Api.dll"]
