FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY contracts/ ./contracts/

COPY src/Commander/Commander.Core/           ./src/Commander/Commander.Core/
COPY src/Commander/Commander.Infrastructure/ ./src/Commander/Commander.Infrastructure/
COPY src/Commander/Commander.Server/         ./src/Commander/Commander.Server/

WORKDIR /repo/src/Commander
RUN dotnet restore Commander.Server/Commander.Server.csproj

RUN dotnet publish Commander.Server/Commander.Server.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5271
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5271 

ENTRYPOINT ["dotnet", "Commander.Server.dll"]
