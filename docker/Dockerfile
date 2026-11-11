# syntax=docker/dockerfile:1
# ============================================================================
# Shared Dockerfile: har service isi se banti hai.
# Kaun si service: build args PROJECT (csproj path) aur ASSEMBLY (dll naam) se.
# Build context = repo root (taaki BuildingBlocks aur props files mil sakein).
# ============================================================================

# Sab stages ke liye .NET version ek jagah. Upgrade = sirf ye line.
ARG DOTNET_VERSION=10.0

# ----------------------------------------------------------------------------
# STAGE 1: build
# SDK image bhari hai (compiler, NuGet). Ye final image mein NAHI jayegi.
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Build ke liye zaroori root files (layer cache ke liye pehle copy)
COPY global.json Directory.Build.props Directory.Packages.props ./

# Saari services aur BuildingBlocks ka source
COPY src/ src/

ARG PROJECT

# Restore: NuGet packages download
RUN dotnet restore "$PROJECT"

# Publish: Release build, output /app/publish
#   --no-restore      : upar restore ho chuka hai
#   UseAppHost=false  : sirf .dll (image chhoti)
RUN dotnet publish "$PROJECT" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ----------------------------------------------------------------------------
# STAGE 2: final (runtime)
# Sirf ASP.NET runtime. SDK, source code, NuGet cache, sab bahar.
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# OS security patches: base image ke apt packages ko latest fixed version par laao
# (Trivy HIGH CVE-2026-109518 jaise fixed issues ke liye).
# Ye root user se chalta hai, isliye USER root, phir wapas non-root.
USER root
RUN set -eux; \
    for i in 1 2 3; do \
      apt-get update && break; \
      echo "apt-get update retry $i"; sleep 5; \
    done; \
    apt-get install -y --no-install-recommends libgssapi-krb5-2; \
    apt-get upgrade -y --no-install-recommends; \
    apt-get clean; \
    rm -rf /var/lib/apt/lists/*

ARG ASSEMBLY
ENV APP_DLL=${ASSEMBLY}.dll

# Kestrel port: non-root user 1024 se upar port hi bind kar sakta hai
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Build stage ka output copy
COPY --from=build /app/publish .

# Security: non-root user (uid 1654, aspnet image mein APP_UID set hota hai)
USER $APP_UID

# exec form: dotnet PID 1 banta hai, docker stop / Ctrl+C sahi chalte hain
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]
