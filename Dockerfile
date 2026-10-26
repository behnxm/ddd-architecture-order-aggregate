# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "DddOrderAggregate.slnx", "./"]
COPY ["src/DddOrderAggregate.Domain/DddOrderAggregate.Domain.csproj", "src/DddOrderAggregate.Domain/"]
COPY ["src/DddOrderAggregate.Application/DddOrderAggregate.Application.csproj", "src/DddOrderAggregate.Application/"]
COPY ["src/DddOrderAggregate.Infrastructure/DddOrderAggregate.Infrastructure.csproj", "src/DddOrderAggregate.Infrastructure/"]
COPY ["src/DddOrderAggregate.Api/DddOrderAggregate.Api.csproj", "src/DddOrderAggregate.Api/"]
COPY ["tests/DddOrderAggregate.UnitTests/DddOrderAggregate.UnitTests.csproj", "tests/DddOrderAggregate.UnitTests/"]
COPY ["tests/DddOrderAggregate.IntegrationTests/DddOrderAggregate.IntegrationTests.csproj", "tests/DddOrderAggregate.IntegrationTests/"]
COPY ["tests/DddOrderAggregate.ArchitectureTests/DddOrderAggregate.ArchitectureTests.csproj", "tests/DddOrderAggregate.ArchitectureTests/"]

# Restore packages
RUN dotnet restore DddOrderAggregate.slnx

# Copy complete repository contents
COPY . .

# Build and publish Api project
RUN dotnet publish "src/DddOrderAggregate.Api/DddOrderAggregate.Api.csproj" -c Release -o /app/publish --no-restore

# Final runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Ensure SQLite storage directory exists with appropriate permissions
USER root
RUN mkdir -p /data && chown -R app:app /data /app
USER app

VOLUME ["/data"]

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ConnectionStrings__Database="Data Source=/data/orders.db"
EXPOSE 8080

COPY --from=build --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "DddOrderAggregate.Api.dll"]
