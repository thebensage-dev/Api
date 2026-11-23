# syntax=docker/dockerfile:1
# Production image: the ASP.NET Core API. PostgreSQL runs next to it (compose.yaml).
# .NET 10 is the LTS: keep these tags on 10.0 (Dependabot ignores .NET majors; move with the next LTS).

# 1. Restore (cached until a project or package version changes), then publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Api/Api.csproj src/Api/
COPY src/Api.Application/Api.Application.csproj src/Api.Application/
COPY src/Api.Domain/Api.Domain.csproj src/Api.Domain/
COPY src/Api.Infrastructure/Api.Infrastructure.csproj src/Api.Infrastructure/
RUN dotnet restore src/Api/Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app --no-restore -p:UseAppHost=false -p:OpenApiGenerateDocuments=false

# 2. Runtime. The "-extra" variant includes ICU, which search needs to strip accents
#    (with invariant globalization .NET can't decompose "á" into "a" + accent).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
COPY --from=build /app ./

# The image's built-in non-root user.
USER $APP_UID
EXPOSE 8080

# Coolify uses this to tell when the app is up. /health also checks the database.
# busybox wget ships with Alpine.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD wget -q --spider "http://127.0.0.1:8080/health" || exit 1

# Applies any new database migrations, optionally adds the demo people, then serves.
ENTRYPOINT ["dotnet", "Api.dll"]
