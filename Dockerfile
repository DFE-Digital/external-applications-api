ARG DOTNET_VERSION=8.0

# ============================================================
# Stage 1 - Build + Install Playwright (Ubuntu SDK)
# ============================================================
# Use Ubuntu-based .NET SDK for Playwright install
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /build
ARG CI
ENV CI=${CI}

# Install Playwright CLI
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy solution
COPY ./src/ ./src/
COPY Directory.Build.props ./
COPY DfE.ExternalApplications.Api.sln ./

# Restore + build
RUN dotnet restore DfE.ExternalApplications.Api.sln
RUN dotnet build ./src/DfE.ExternalApplications.Api --configuration Release --no-restore

# Install Playwright browsers + OS dependencies using Ubuntu (works)
RUN playwright install --with-deps

# Publish final output
RUN dotnet publish ./src/DfE.ExternalApplications.Api --configuration Release --no-build -o /app


# ============================================================
# Stage 2 - EF Migration Builder
# ============================================================
FROM build AS efbuilder
WORKDIR /build

ENV PATH=$PATH:/root/.dotnet/tools
RUN dotnet tool install --global dotnet-ef --version 8.*
RUN mkdir /sql
RUN dotnet ef migrations bundle -r linux-x64 \
      --configuration Release \
      --project ./src/DfE.ExternalApplications.Api \
      --no-build -o /sql/migratedb


# ============================================================
# Stage 3 - Init Container
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS initcontainer
WORKDIR /sql

COPY --from=efbuilder /sql /sql
COPY --from=build /app/appsettings* /sql/
COPY --from=build /app/appsettings* /DfE.ExternalApplications.Api/


# ============================================================
# Stage 4 - Final Runtime (Azure Linux) + Playwright browsers
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final

WORKDIR /app

# Copy published API
COPY --from=build /app /app

# Copy Playwright installed browsers + driver
COPY --from=build /root/.cache/ms-playwright /root/.cache/ms-playwright
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
ENV PATH="${PATH}:/root/.dotnet/tools"

# Entrypoint script
COPY script/api-docker-entrypoint.sh /app/docker-entrypoint.sh
RUN sed -i 's/\r$//' /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

USER $APP_UID
ENTRYPOINT ["/app/docker-entrypoint.sh"]
