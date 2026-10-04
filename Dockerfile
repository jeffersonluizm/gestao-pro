# API do Gestão Pro (usada pelo Render: render.yaml). Build em duas etapas; a imagem final roda sem root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/GestaoPro.API/GestaoPro.API.csproj
RUN dotnet publish src/GestaoPro.API/GestaoPro.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

# Segredos e banco vêm de variáveis de ambiente (ver docs/deploy.md); nada de appsettings.Local.json na imagem.
ENV ASPNETCORE_URLS=http://+:10000     ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

# Usuário sem privilégios que já vem na imagem do ASP.NET.
USER app

ENTRYPOINT ["dotnet", "GestaoPro.API.dll"]
