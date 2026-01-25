FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# ffmpeg libgdiplus required for ffmpeg
RUN apt-get update && apt-get install ffmpeg libgdiplus -y


FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-aot AS build
WORKDIR /src
COPY .git .git
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["gen/UndercutF1.Data.SourceGeneration/UndercutF1.Data.SourceGeneration.csproj", "gen/UndercutF1.Data.SourceGeneration/"]
COPY ["gen/UndercutF1.Console.SourceGeneration/UndercutF1.Console.SourceGeneration.csproj", "gen/UndercutF1.Console.SourceGeneration/"]
COPY ["UndercutF1.Data/UndercutF1.Data.csproj", "UndercutF1.Data/"]
COPY ["UndercutF1.Console/UndercutF1.Console.csproj", "UndercutF1.Console/"]
RUN dotnet restore "UndercutF1.Console/UndercutF1.Console.csproj"
COPY . .
WORKDIR "/src/UndercutF1.Console"
ARG TARGETARCH
RUN dotnet publish "UndercutF1.Console.csproj" -o /app/publish -a "$TARGETARCH" /p:UseAppHost=false /p:PublicRelease=true /p:PublishAot=false /p:SelfContained=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV UNDERCUTF1_DATADIRECTORY=/data
ENV UNDERCUTF1_LOGDIRECTORY=/logs

ENTRYPOINT ["dotnet", "undercutf1.dll"]
