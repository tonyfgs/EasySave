FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5050

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LogCentralizer/LogCentralizer.csproj", "LogCentralizer/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "LogCentralizer/LogCentralizer.csproj"
COPY LogCentralizer/ LogCentralizer/
COPY Shared/ Shared/
WORKDIR "/src/LogCentralizer"
RUN dotnet build "LogCentralizer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LogCentralizer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5050
ENV LogDirectory=/app/logs

# Health check using wget (available in aspnet image) instead of curl
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5050/api/health || exit 1

ENTRYPOINT ["dotnet", "LogCentralizer.dll"]
