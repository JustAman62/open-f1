FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG configuration=Release
WORKDIR /src
COPY .editorconfig Directory.* ./
COPY ["OpenF1.Data/OpenF1.Data.csproj", "OpenF1.Data/"]
COPY ["OpenF1.Data.Ingest/OpenF1.Data.Ingest.csproj", "OpenF1.Data.Ingest/"]
RUN dotnet restore "OpenF1.Data.Ingest/OpenF1.Data.Ingest.csproj"
COPY . .
WORKDIR "/src/OpenF1.Data.Ingest"
RUN dotnet build "OpenF1.Data.Ingest.csproj" -c $configuration -o /app/build --no-restore

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "OpenF1.Data.Ingest.csproj" -c $configuration -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenF1.Data.Ingest.dll"]
