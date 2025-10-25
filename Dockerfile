# ============================
# Stage 1: Build
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj và restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ mã nguồn còn lại
COPY . ./

# Build project
RUN dotnet publish -c Release -o /out

# ============================
# Stage 2: Runtime
# ============================
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /out ./

ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "TeleCodeFeeBot.dll"]
