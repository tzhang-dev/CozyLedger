FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/src/CozyLedger.Api/CozyLedger.Api.csproj backend/src/CozyLedger.Api/
COPY backend/src/CozyLedger.Infrastructure/CozyLedger.Infrastructure.csproj backend/src/CozyLedger.Infrastructure/
COPY backend/src/CozyLedger.Domain/CozyLedger.Domain.csproj backend/src/CozyLedger.Domain/
RUN dotnet restore backend/src/CozyLedger.Api/CozyLedger.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/src/CozyLedger.Api/CozyLedger.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CozyLedger.Api.dll"]
