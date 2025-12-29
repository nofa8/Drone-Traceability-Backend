# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0.302-bookworm-slim AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything and publish
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0.7-bookworm-slim
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5101

ENV ASPNETCORE_URLS=http://+:5101

ENTRYPOINT ["dotnet", "dTITAN.Backend.dll"]
