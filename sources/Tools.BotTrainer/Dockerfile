# Build context: solution root (sources/)
#   docker compose --profile tools run --rm bottrainer <befehl> [optionen]
#   (siehe scripts/train-model.ps1 / .sh)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Zentrale Paketversionen und Build-Einstellungen (vor dem Restore nötig)
COPY Directory.Build.props Directory.Packages.props ./

# Restore first (layer cache): only the project files the trainer depends on
COPY Tools.BotTrainer/Tools.BotTrainer.csproj       Tools.BotTrainer/
COPY Logic.Bot/Logic.Bot.csproj                     Logic.Bot/
COPY Logic.Shared/Logic.Shared.csproj               Logic.Shared/
COPY Data.DartContext/Data.DartContext.csproj       Data.DartContext/
COPY Shared.Interfaces/Shared.Interfaces.csproj     Shared.Interfaces/
COPY Shared.Models/Shared.Models.csproj             Shared.Models/
COPY Shared.Enums/Shared.Enums.csproj               Shared.Enums/
RUN dotnet restore Tools.BotTrainer/Tools.BotTrainer.csproj

COPY . .
RUN dotnet publish Tools.BotTrainer/Tools.BotTrainer.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Läuft bewusst als root: der Trainer schreibt in den vom Host eingebundenen Ordner ml_data,
# dessen Besitzer je nach Host (Windows, Raspberry Pi) unterschiedlich ist. Kurzlebiger Werkzeug-Container, kein Dienst.
ENV MlData__Path=/data/ml

ENTRYPOINT ["dotnet", "BotTrainer.dll"]
