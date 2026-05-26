# syntax=docker/dockerfile:1
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EduConnect.sln ./
COPY src/EduConnect.Domain/EduConnect.Domain.csproj src/EduConnect.Domain/
COPY src/EduConnect.Application/EduConnect.Application.csproj src/EduConnect.Application/
COPY src/EduConnect.Infrastructure/EduConnect.Infrastructure.csproj src/EduConnect.Infrastructure/
COPY src/EduConnect.Api/EduConnect.Api.csproj src/EduConnect.Api/
RUN dotnet restore src/EduConnect.Api/EduConnect.Api.csproj

COPY src/ src/
RUN dotnet publish src/EduConnect.Api/EduConnect.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Npgsql GSSAPI/Kerberos negotiate'i denedigi icin bu kutuphaneyi bekliyor;
# yoksa "Cannot load library libgssapi_krb5.so.2" hata logu basiyor (fonksiyonel sorun degil, gurultu).
RUN apt-get update && apt-get install -y --no-install-recommends \
        libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN mkdir -p /app/uploads

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "EduConnect.Api.dll"]
