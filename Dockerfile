FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Tripwalaah.LocationService.slnx ./
COPY Directory.Build.props ./
COPY global.json ./
COPY src/Tripwalaah.LocationService.Domain/Tripwalaah.LocationService.Domain.csproj src/Tripwalaah.LocationService.Domain/
COPY src/Tripwalaah.LocationService.Application/Tripwalaah.LocationService.Application.csproj src/Tripwalaah.LocationService.Application/
COPY src/Tripwalaah.LocationService.Infrastructure/Tripwalaah.LocationService.Infrastructure.csproj src/Tripwalaah.LocationService.Infrastructure/
COPY src/Tripwalaah.LocationService.Api/Tripwalaah.LocationService.Api.csproj src/Tripwalaah.LocationService.Api/

RUN dotnet restore Tripwalaah.LocationService.slnx

COPY src/ src/
RUN dotnet publish src/Tripwalaah.LocationService.Api/Tripwalaah.LocationService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5000
ENV PORT=5000
EXPOSE 5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tripwalaah.LocationService.Api.dll"]
