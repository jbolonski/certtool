FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Client/Client.csproj src/Client/
COPY src/Server/Server.csproj src/Server/
RUN dotnet restore src/Server/Server.csproj
COPY . .
WORKDIR /src/src/Server
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Copy migration SQL scripts
COPY db ./db
ENTRYPOINT ["dotnet", "Server.dll"]
