# Build context: solution root (sources/)
#   docker build -f Web.App/Dockerfile -t dartmetrics-web .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (layer cache): only the project files the web app depends on
COPY Web.App/Web.App/Web.App.csproj                 Web.App/Web.App/
COPY Web.App/Web.App.Client/Web.App.Client.csproj   Web.App/Web.App.Client/
COPY Web.Shared/Web.Shared.csproj                   Web.Shared/
COPY Shared.Interfaces/Shared.Interfaces.csproj     Shared.Interfaces/
COPY Shared.Models/Shared.Models.csproj             Shared.Models/
COPY Shared.Enums/Shared.Enums.csproj               Shared.Enums/
COPY Data.DartContext/Data.DartContext.csproj       Data.DartContext/
RUN dotnet restore Web.App/Web.App/Web.App.csproj

COPY . .
RUN dotnet publish Web.App/Web.App/Web.App.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Data protection keys (cookies, session store) - mounted as volume so logins survive restarts
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet

# non-root user shipped with the official .NET images
USER $APP_UID

ENTRYPOINT ["dotnet", "Web.App.dll"]
