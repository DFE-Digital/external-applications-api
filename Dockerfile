ARG DOTNET_VERSION=8.0

# ==============================================
# Base SDK
# ==============================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-azurelinux3.0 AS builder
ARG CI
ENV CI=${CI}
WORKDIR /build

# Mount GitHub Token as a Docker secret so that NuGet Feed can be accessed
RUN --mount=type=secret,id=github_token dotnet nuget add source \
    --username USERNAME \
    --password $(cat /run/secrets/github_token) \
    --store-password-in-clear-text \
    --name github "https://nuget.pkg.github.com/DFE-Digital/index.json"

ARG PROJECT_NAME="DfE.ExternalApplications.Api"
COPY ./${PROJECT_NAME}.sln .
COPY ./Directory.Build.props .
COPY ./src/ ./src/
RUN dotnet restore
RUN dotnet build ./src/${PROJECT_NAME} -c Release -p:CI=${CI} --no-restore
RUN dotnet publish ./src/${PROJECT_NAME} -c Release -o /app --no-build

# ==============================================
# Entity Framework: Migration Builder
# ==============================================
FROM builder AS efbuilder
WORKDIR /build
ARG DOTNET_EF_TAG=8.0.8
ARG PROJECT_NAME="DfE.ExternalApplications.Api"

ENV PATH=$PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef  --version ${DOTNET_EF_TAG}
RUN mkdir /sql
RUN dotnet ef migrations bundle -r linux-x64 --configuration Release -p ${PROJECT_NAME} --no-build -o /sql/migratedb

# ==============================================
# Entity Framework: Migration Runner
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS initcontainer
WORKDIR /sql
COPY --from=efbuilder /sql /sql
COPY --from=builder /app/appsettings* /DfE.ExternalApplications.Api/

# ==============================================
# Application
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
LABEL org.opencontainers.image.source="https://github.com/DFE-Digital/external-applications-api"
LABEL org.opencontainers.image.description="External Applications - API"

COPY --from=builder /app /app
COPY ./script/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x ./docker-entrypoint.sh

USER $APP_UID
