FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./src ./src
WORKDIR /src/src/Api

RUN dotnet restore "OnlineGame.AuthService.Api.csproj"
RUN dotnet publish "OnlineGame.AuthService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OnlineGame.AuthService.Api.dll"]