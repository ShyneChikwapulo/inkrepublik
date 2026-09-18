# ============================================================================
# INKREPUBLIK — Production Dockerfile
# ============================================================================
#
# Multi-stage build:
#   1. SDK stage: restore, build, publish the app.
#   2. Runtime stage: minimal ASP.NET image with just the published output.
#
# Only the runtime stage ends up in the final image (~220 MB), not the SDK
# (~900 MB). This is the standard .NET container pattern.
# ============================================================================

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first and restore. This layer gets cached and only
# invalidated when a .csproj changes — not on every source file edit.
COPY Inkrepublik.slnx ./
COPY src/Inkrepublik.Domain/Inkrepublik.Domain.csproj          src/Inkrepublik.Domain/
COPY src/Inkrepublik.Data/Inkrepublik.Data.csproj              src/Inkrepublik.Data/
COPY src/Inkrepublik.Services/Inkrepublik.Services.csproj      src/Inkrepublik.Services/
COPY src/Inkrepublik.Web/Inkrepublik.Web.csproj                src/Inkrepublik.Web/
COPY tests/Inkrepublik.Tests/Inkrepublik.Tests.csproj          tests/Inkrepublik.Tests/

RUN dotnet restore Inkrepublik.slnx

# Now copy the rest of the source and publish.
COPY . .
RUN dotnet publish src/Inkrepublik.Web/Inkrepublik.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# These settings help the app behave better in containers:
#   - DOTNET_RUNNING_IN_CONTAINER: tell .NET it's in a container
#   - ASPNETCORE_URLS: listen on 8080 (Render's default)
#   - DOTNET_gcServer: use workstation GC (lighter on memory for a
#     single-app container — server GC is meant for multi-core workloads)
#   - DOTNET_EnableDiagnostics: disable diagnostic IPC (smaller attack surface)
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=0 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Copy the published output from the build stage.
COPY --from=build /app/publish .

# Create writable directories AND set ownership. Must happen AFTER the COPY,
# because COPY runs as root and would overwrite our changes to the folder.
# Also chowns the entire wwwroot so the app can create uploads subfolders.
RUN mkdir -p /app/data /app/wwwroot/uploads \
    && chown -R app:app /app/data /app/wwwroot/uploads

# Run as a non-root user for security. The .NET base image ships with an
# 'app' user (UID 1654) since .NET 8.
USER app

ENTRYPOINT ["dotnet", "Inkrepublik.Web.dll"]