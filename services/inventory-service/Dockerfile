FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# When built from repo root as context, copy service sources and the shared proto
# into the build context so Grpc.Tools can find the .proto files referenced
COPY services/inventory-service/src ./src
COPY proto /proto
WORKDIR /src/src/Api

RUN dotnet restore "OnlineGame.InventoryService.Api.csproj"
RUN dotnet publish "OnlineGame.InventoryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# ASPNETCORE_URLS omitido de proposito: os endpoints (8080 REST/Http1, 8081 gRPC/Http2)
# sao definidos por codigo no Program.cs (ListenAnyIP com protocolo por porta).
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlineGame.InventoryService.Api.dll"]
