ARG DOTNET_VERSION=8.0

# ============================================================
# Stage 1 - Build + Install Playwright (Ubuntu SDK)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-jammy AS build
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
RUN dotnet build ./src/DfE.ExternalApplications.Api -c Release --no-restore

# Install Playwright browsers + OS dependencies (Ubuntu!)
RUN playwright install --with-deps

# Publish final output
RUN dotnet publish ./src/DfE.ExternalApplications.Api -c Release --no-build -o /app


# ============================================================
# Stage 2 - EF Migration Builder
# ============================================================
FROM build AS efbuilder
WORKDIR /build

ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 8.*
RUN mkdir /sql
RUN dotnet ef migrations bundle -r linux-x64 \
      -c Release \
      --project ./src/DfE.ExternalApplications.Api \
      --no-build -o /sql/migratedb


# ============================================================
# Stage 3 - Init Container (Keeps Azure Linux if needed)
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS initcontainer
WORKDIR /sql

COPY --from=efbuilder /sql /sql
COPY --from=build /app/appsettings* /sql/
COPY --from=build /app/appsettings* /DfE.ExternalApplications.Api/


# ============================================================
# Stage 4 - Final Runtime (Ubuntu) + Playwright Runtime Support
# ============================================================

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-jammy AS final
WORKDIR /app

# Install Playwright required system dependencies
RUN apt-get update && \
    apt-get install -y \
        libnss3 \
        libatk1.0-0 \
        libatk-bridge2.0-0 \
        libcups2 \
        libdbus-1-3 \
        libdrm2 \
        libxcomposite1 \
        libxdamage1 \
        libxrandr2 \
        libgbm1 \
        libasound2 \
        libxshmfence1 \
        libxkbcommon0 \
        libxext6 \
        libxfixes3 \
        libx11-6 \
        libx11-xcb1 \
        libglib2.0-0 \
        libgl1 \
        libpango-1.0-0 \
        libpangocairo-1.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Copy app + Playwright browsers
COPY --from=build /app /app
COPY --from=build /root/.cache/ms-playwright /home/app/.cache/ms-playwright
RUN chmod -R 755 /home/app/.cache/ms-playwright

COPY script/api-docker-entrypoint.sh /app/docker-entrypoint.sh
RUN sed -i 's/\r$//' /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080

USER $APP_UID
ENTRYPOINT ["/app/docker-entrypoint.sh"]