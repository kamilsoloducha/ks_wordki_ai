# syntax=docker/dockerfile:1
# Build from repo root of Bff:  docker build -t wordki-bff -f Dockerfile .
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/ ./

RUN dotnet publish Wordki.Bff.Api/Wordki.Bff.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Wordki.Bff.Api.dll"]
