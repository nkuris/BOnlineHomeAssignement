# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy the full repository into the build context. This avoids issues with
# Docker build contexts that make relative project-file COPY unreliable.
# It trades off some cache granularity for robustness in the Visual Studio
# compose build environment.
COPY . .
# Restore and build the server project. Support two common build contexts:
# - If the build context is the server folder, the csproj is at ./BOnlineHomeAssignement.Server.csproj
# - If the build context is the solution root, the csproj is at ./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj
RUN if [ -f ./BOnlineHomeAssignement.Server.csproj ]; then \
	  echo "Using ./BOnlineHomeAssignement.Server.csproj"; \
	  dotnet restore "./BOnlineHomeAssignement.Server.csproj" && dotnet build "./BOnlineHomeAssignement.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build; \
	elif [ -f ./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj ]; then \
	  echo "Using ./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj"; \
	  dotnet restore "./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj" && dotnet build "./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build; \
	else \
	  echo "Project file not found in build context" >&2; exit 1; \
	fi

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN set -eux \
	&& CSPROJ_PATH=$(sh -c "if [ -f ./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj ]; then echo ./BOnlineHomeAssignement.Server/BOnlineHomeAssignement.Server.csproj; elif [ -f ./BOnlineHomeAssignement.Server.csproj ]; then echo ./BOnlineHomeAssignement.Server.csproj; else find . -maxdepth 3 -name 'BOnlineHomeAssignement.Server.csproj' | head -n1; fi") \
	&& if [ -z "$CSPROJ_PATH" ]; then echo 'Project file not found for publish' >&2; exit 1; fi \
	&& dotnet publish "$CSPROJ_PATH" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
	&& if [ -f ./BOnlineHomeAssignement.Server.SqlTools/BOnlineHomeAssignement.Server.SqlTools.csproj ]; then \
		 dotnet publish ./BOnlineHomeAssignement.Server.SqlTools/BOnlineHomeAssignement.Server.SqlTools.csproj -c $BUILD_CONFIGURATION -o /app/publish/tools/checksa /p:UseAppHost=false; \
	  elif [ -f ./BOnlineHomeAssignement.Server.SqlTools/CheckSa.csproj ]; then \
		 dotnet publish ./BOnlineHomeAssignement.Server.SqlTools/CheckSa.csproj -c $BUILD_CONFIGURATION -o /app/publish/tools/checksa /p:UseAppHost=false; \
	  else \
		 echo "No sqltools project found, skipping"; \
	  fi

# Development stage for dotnet watch - inherits cached packages from build
FROM build AS dev-base
WORKDIR /workspace
# Install curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
EXPOSE 80

# Development stage with volumes mounted and dotnet watch ready
FROM dev-base AS dev
# Copy the entire source (will be overridden by volume mount in compose)
COPY . /workspace/
# Copy the wait-for-services script
COPY wait-for-services.sh /workspace/wait-for-services.sh
# Normalize line endings and make executable
RUN if [ -f /workspace/wait-for-services.sh ]; then tr -d '\r' < /workspace/wait-for-services.sh > /workspace/wait-for-services.sh.tmp && mv /workspace/wait-for-services.sh.tmp /workspace/wait-for-services.sh || true; fi
RUN chmod +x /workspace/wait-for-services.sh || true

# Runtime stage - base for production
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# Listen on port 5000 to match compose mapping
EXPOSE 5000
# Note: Run as root for dev container (simplifies entrypoint and healthcheck execution)

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Install curl for healthcheck - run as root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY wait-for-services.sh /app/wait-for-services.sh
# Normalize line endings and make executable
RUN if [ -f /app/wait-for-services.sh ]; then tr -d '\r' < /app/wait-for-services.sh > /app/wait-for-services.sh.tmp && mv /app/wait-for-services.sh.tmp /app/wait-for-services.sh || true; fi
RUN chmod +x /app/wait-for-services.sh || true

# Healthcheck should target the application's health endpoint on container port 5000.
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=5 \
  CMD curl -f http://localhost:5000/health || exit 1

# Start script will wait for configured services and then run the app.
ENTRYPOINT ["/app/wait-for-services.sh"]