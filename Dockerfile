# ============================
# Stage 1: Build
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files
COPY *.csproj ./
COPY ./services ./services
COPY ./Config ./Config
COPY ./Program.cs ./

# Restore dependencies
RUN dotnet restore

# Copy remaining files
COPY . ./

# Build project
RUN dotnet publish -c Release -o /out

# ============================
# Stage 2: Runtime
# ============================
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy build output
COPY --from=build /out ./

# Set environment variables (Render sẽ set ở dashboard)
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Run bot
ENTRYPOINT ["dotnet", "TeleCodeFeeBot.dll"]
