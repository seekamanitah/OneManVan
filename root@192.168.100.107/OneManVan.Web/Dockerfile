# OneManVan Web - Multi-stage Dockerfile for .NET 10
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["OneManVan.Web/OneManVan.Web.csproj", "OneManVan.Web/"]
COPY ["OneManVan.Shared/OneManVan.Shared.csproj", "OneManVan.Shared/"]

# Restore dependencies
RUN dotnet restore "OneManVan.Web/OneManVan.Web.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/OneManVan.Web"
RUN dotnet build "OneManVan.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "OneManVan.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite fallback and config
RUN mkdir -p /app/AppData && chmod 777 /app/AppData

ENTRYPOINT ["dotnet", "OneManVan.Web.dll"]
