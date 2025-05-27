# Set the major version of dotnet
ARG DOTNET_VERSION=8.0

# Stage 1 - Build the app using the dotnet SDK
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-azurelinux3.0 AS build
WORKDIR /build

# Mount GitHub Token as a Docker secret so that NuGet Feed can be accessed
RUN --mount=type=secret,id=github_token dotnet nuget add source --username USERNAME --password $(cat /run/secrets/github_token) --store-password-in-clear-text --name github "https://nuget.pkg.github.com/DFE-Digital/index.json"

# Copy the application code
COPY ./src/ ./

# Build and publish the dotnet solution
RUN dotnet restore DfE.ExternalApplications.Api && \
    dotnet build DfE.ExternalApplications.Api --no-restore -c Release && \
    dotnet publish DfE.ExternalApplications.Api --no-build -o /app

# ==============================================
# Entity Framework: Migration Builder
# ==============================================
FROM build AS efbuilder
WORKDIR /build
ARG DOTNET_EF_TAG=8.0.8
ARG PROJECT_NAME="DfE.ExternalApplications.Api"

ENV PATH=$PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef
RUN mkdir /sql
RUN dotnet ef migrations bundle -r linux-x64 \
      --configuration Release \
      --project DfE.ExternalApplications.Api \
      --no-build -o /sql/migratedb

# ==============================================
# Entity Framework: Migration Runner
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS initcontainer
WORKDIR /sql
COPY --from=efbuilder /sql /sql
COPY --from=build /app/appsettings* /DfE.ExternalApplications.Api/

# Stage 3 - Build a runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
WORKDIR /app
LABEL org.opencontainers.image.source="https://github.com/DFE-Digital/external-applications-api"
LABEL org.opencontainers.image.description="External Applications - Api"

COPY --from=build /app /app
COPY ./script/api-docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh

USER $APP_UID
