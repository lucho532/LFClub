FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /codigo

COPY Directory.Build.props ./
COPY src/LFClub.Api/LFClub.Api.csproj src/LFClub.Api/
COPY src/LFClub.Aplicacion/LFClub.Aplicacion.csproj src/LFClub.Aplicacion/
COPY src/LFClub.Dominio/LFClub.Dominio.csproj src/LFClub.Dominio/
COPY src/LFClub.Infraestructura/LFClub.Infraestructura.csproj src/LFClub.Infraestructura/
RUN dotnet restore src/LFClub.Api/LFClub.Api.csproj

COPY src/ src/
RUN dotnet publish src/LFClub.Api/LFClub.Api.csproj -c Release -o /publicado --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilacion /publicado ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "LFClub.Api.dll"]
