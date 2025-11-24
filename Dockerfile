FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Todo.Api/Todo.Api.csproj Todo.Api/
COPY Todo.Api.Tests/Todo.Api.Tests.csproj Todo.Api.Tests/
COPY todo.slnx ./
RUN dotnet restore Todo.Api/Todo.Api.csproj

COPY . .
WORKDIR /src/Todo.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Todo.Api.dll"]

