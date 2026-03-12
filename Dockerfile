# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia los archivos de solución y .csproj de los proyectos principales
COPY *.sln .
COPY MoneyTracker.Tests/*.csproj MoneyTracker.Tests/
COPY MoneyTracker.Application/*.csproj MoneyTracker.Application/
COPY MoneyTracker.Domain/*.csproj MoneyTracker.Domain/
COPY MoneyTracker.Infrastructure/*.csproj MoneyTracker.Infrastructure/
COPY MoneyTracker.UI/*.csproj MoneyTracker.UI/

# Restaurar dependencias
RUN dotnet restore

# Copiar el resto del contenido
COPY . .

# Publicar el proyecto principal
RUN dotnet publish MoneyTracker.UI/MoneyTracker.UI.csproj -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8091

ENTRYPOINT ["dotnet", "MoneyTracker.UI.dll"]