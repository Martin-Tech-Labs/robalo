FROM mcr.microsoft.com/dotnet/sdk:11.0-alpine3.24 AS build
WORKDIR /src

COPY src/Robalo.Controller.Api/Robalo.Controller.Api.csproj ./
RUN dotnet restore Robalo.Controller.Api.csproj

# Copy the scaffold's source and configuration without local bin/obj output.
COPY src/Robalo.Controller.Api/*.cs ./
COPY src/Robalo.Controller.Api/appsettings*.json ./
RUN dotnet publish Robalo.Controller.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0-alpine3.24 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=5010
EXPOSE 5010
USER $APP_UID
ENTRYPOINT ["dotnet", "Robalo.Controller.Api.dll"]
