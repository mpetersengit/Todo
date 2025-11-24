FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for dependency restoration
COPY Todo.Api/Todo.Api.csproj Todo.Api/
COPY Todo.Api.Tests/Todo.Api.Tests.csproj Todo.Api.Tests/
COPY todo.slnx ./

# Restore dependencies
RUN dotnet restore Todo.Api/Todo.Api.csproj

# Copy all source files
COPY . .

# Build and publish
WORKDIR /src/Todo.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create directories for data and logs with proper permissions
RUN mkdir -p /app/data /app/logs && \
    chmod 755 /app/data /app/logs

# Copy published application
COPY --from=build /app/publish .

# Expose data directory as volume for persistence (optional - users can mount their own)
VOLUME ["/app/data"]

ENTRYPOINT ["dotnet", "Todo.Api.dll"]

