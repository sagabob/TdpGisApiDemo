# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TdpGis.Endpoints/TdpGis.Endpoints.csproj", "TdpGis.Endpoints/"]
COPY ["TdpGis.AdminApplication/TdpGis.AdminApplication.csproj", "TdpGis.AdminApplication/"]
COPY ["TdpGis.Domain/TdpGis.Domain.csproj", "TdpGis.Domain/"]
COPY ["TdpGis.Infrastructure/TdpGis.Infrastructure.csproj", "TdpGis.Infrastructure/"]
COPY ["TdpGis.Application/TdpGis.Application.csproj", "TdpGis.Application/"]
RUN dotnet restore "./TdpGis.Endpoints/TdpGis.Endpoints.csproj"
COPY . .
WORKDIR "/src/TdpGis.Endpoints"
RUN dotnet build "./TdpGis.Endpoints.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TdpGis.Endpoints.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
# Npgsql can dlopen libgssapi_krb5 (Kerberos); slim runtime images omit it — required on Linux or PostgreSQL init fails.
USER root
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TdpGis.Endpoints.dll"]