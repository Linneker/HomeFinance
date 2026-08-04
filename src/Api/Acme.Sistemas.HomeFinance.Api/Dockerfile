# Build a partir da raiz do repositório:
#   docker build -f src/Api/Acme.Sistemas.HomeFinance.Api/Dockerfile -t acme/homefinance-api:dev .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar apenas os csproj primeiro aproveita o cache de camadas do Docker: mudança de código
# não invalida a restauração de pacotes.
COPY NuGet.config ./
COPY src/Api/Acme.Sistemas.HomeFinance.Api/*.csproj              src/Api/Acme.Sistemas.HomeFinance.Api/
COPY src/Service/Acme.Sistemas.Services/*.csproj                 src/Service/Acme.Sistemas.Services/
COPY src/Service/Acme.Sistemas.Core/*.csproj                     src/Service/Acme.Sistemas.Core/
COPY src/Service/Acme.Sistemas.Domain/*.csproj                   src/Service/Acme.Sistemas.Domain/
COPY src/Data/Acme.Sistemas.Infrastructure/*.csproj              src/Data/Acme.Sistemas.Infrastructure/
COPY src/Data/Acme.Sistemas.Repository/*.csproj                  src/Data/Acme.Sistemas.Repository/
COPY src/Data/Acme.Sistemas.ExternalIntegration/*.csproj         src/Data/Acme.Sistemas.ExternalIntegration/
RUN dotnet restore src/Api/Acme.Sistemas.HomeFinance.Api/Acme.Sistemas.HomeFinance.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/Acme.Sistemas.HomeFinance.Api/Acme.Sistemas.HomeFinance.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Usuário sem privilégio: exigência de OWASP, regra 26 do blueprint.
USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Acme.Sistemas.HomeFinance.Api.dll"]
