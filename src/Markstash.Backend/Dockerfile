FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.Config ./
COPY src/Markstash.Contracts/Markstash.Contracts.csproj src/Markstash.Contracts/
COPY src/Markstash.Backend/Markstash.Backend.csproj src/Markstash.Backend/
RUN dotnet restore src/Markstash.Backend/Markstash.Backend.csproj --configfile NuGet.Config

COPY src/Markstash.Contracts/ src/Markstash.Contracts/
COPY src/Markstash.Backend/ src/Markstash.Backend/
RUN dotnet publish src/Markstash.Backend/Markstash.Backend.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Markstash.Backend.dll"]
