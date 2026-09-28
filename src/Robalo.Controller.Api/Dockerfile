FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine3.24 AS build
WORKDIR /src


COPY src/Robalo.Controller.Api/ Robalo.Controller.Api/
COPY src/Robalo.Common/ Robalo.Common/

RUN dotnet publish Robalo.Controller.Api/Robalo.Controller.Api.csproj -c Release -o /app/publish /p:UseAppHost=false -p:ErrorOnMissingProjectReference=true

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine3.24 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

USER $APP_UID
ENTRYPOINT ["dotnet", "Robalo.Controller.Api.dll"]
