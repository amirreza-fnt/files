# Multi-stage build for Linux (AlmaLinux / Docker).
# Framework-dependent image — matches the systemd deployment (runtime installed on the host).

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FileStorage.Common/FileStorage.Common.csproj src/FileStorage.Common/
COPY src/FileStorage.Domain/FileStorage.Domain.csproj src/FileStorage.Domain/
COPY src/FileStorage.Application/FileStorage.Application.csproj src/FileStorage.Application/
COPY src/FileStorage.Infrastructure/FileStorage.Infrastructure.csproj src/FileStorage.Infrastructure/
COPY src/FileStorage.Api/FileStorage.Api.csproj src/FileStorage.Api/
RUN dotnet restore src/FileStorage.Api/FileStorage.Api.csproj

COPY src/ src/
RUN dotnet publish src/FileStorage.Api/FileStorage.Api.csproj -c Release -o /out --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:6000
EXPOSE 6000

# Storage volume lives outside the container filesystem
VOLUME /var/lib/filestorage

ENTRYPOINT ["dotnet", "FileStorage.Api.dll"]
