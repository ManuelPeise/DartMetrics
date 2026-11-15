# Build context: solution root (sources/)
#   docker build -f Web.Core/Dockerfile -t dartmetrics-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Zentrale Paketversionen und Build-Einstellungen (vor dem Restore nötig)
COPY Directory.Build.props Directory.Packages.props ./

# Restore first (layer cache): only the project files the API depends on
COPY Web.Core/Web.Core.csproj                         Web.Core/
COPY Service.Api/Service.Api.csproj                   Service.Api/
COPY Data.DartContext/Data.DartContext.csproj         Data.DartContext/
COPY Logic.Administration/Logic.Administration.csproj Logic.Administration/
COPY Logic.Authentication/Logic.Authentication.csproj Logic.Authentication/
COPY Logic.Bot/Logic.Bot.csproj                       Logic.Bot/
COPY Logic.Profile/Logic.Profile.csproj               Logic.Profile/
COPY Logic.Shared/Logic.Shared.csproj                 Logic.Shared/
COPY Logic.Statistics/Logic.Statistics.csproj         Logic.Statistics/
COPY Logic.Tournament/Logic.Tournament.csproj         Logic.Tournament/
COPY Logic.Training/Logic.Training.csproj             Logic.Training/
COPY Shared.Enums/Shared.Enums.csproj                 Shared.Enums/
COPY Shared.Interfaces/Shared.Interfaces.csproj       Shared.Interfaces/
COPY Shared.Models/Shared.Models.csproj               Shared.Models/
RUN dotnet restore Web.Core/Web.Core.csproj

COPY . .
RUN dotnet publish Web.Core/Web.Core.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# non-root user shipped with the official .NET images
USER $APP_UID

ENTRYPOINT ["dotnet", "Web.Core.dll"]
