FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine3.24 AS build
WORKDIR /src

COPY . /src

RUN dotnet tool restore

RUN dotnet run cake.cs -- --target PublishController --configuration=Release

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine3.24 AS runtime
WORKDIR /app
COPY --from=build /src/app/publish ./

HEALTHCHECK --interval=15s --timeout=10s --start-period=5s --retries=2  \  
   CMD wget -q -O /dev/null http://localhost:5010/_system/health || exit 1

USER $APP_UID
ENTRYPOINT ["dotnet", "Robalo.Controller.Api.dll"]