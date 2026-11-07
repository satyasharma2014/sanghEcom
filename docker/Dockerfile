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

# Build ke liye zaroori root files:
#   global.json            -> SDK version lock
#   Directory.Build.props  -> common settings (net10.0, nullable, analyzers)
#   Directory.Packages.props -> NuGet versions (Central Package Management)
COPY global.json Directory.Build.props Directory.Packages.props ./

# Saari services aur BuildingBlocks ka source.
# Note: pehle wale "csproj-only restore" se simple, aur cache ka trade-off hai (neeche note).
COPY src/ src/

# Build args (compose se aate hain)
ARG PROJECT

# Restore: NuGet packages download (container ke andar)
RUN dotnet restore "$PROJECT"

# Publish: Release build + output /app/publish
#   --no-restore : upar restore ho chuka hai, dobara nahi
#   UseAppHost=false : .exe nahi, sirf .dll (image chhoti)
RUN dotnet publish "$PROJECT" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ----------------------------------------------------------------------------
# STAGE 2: final (runtime)
# Sirf ASP.NET runtime. SDK, source code, NuGet cache, sab bahar.
# ----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
WORKDIR /app

# Build args ko runtime stage mein dobara declare karna zaroori hai
ARG ASSEMBLY
ENV APP_DLL=${ASSEMBLY}.dll

# Kestrel port: non-root user 1024 se upar port hi bind kar sakta hai
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Build stage ka output copy
COPY --from=build /app/publish .

# Security: root ki jagah built-in non-root user (uid 1654). aspnet image mein APP_UID set hota hai.
USER $APP_UID

# exec form ke bina Ctrl+C / docker stop sahi se kaam nahi karte.
# "exec" se dotnet hi PID 1 banta hai.
ENTRYPOINT ["sh", "-c", "exec dotnet $APP_DLL"]
