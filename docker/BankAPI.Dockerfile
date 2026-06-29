FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
ARG APP_DIR=BankApp

COPY ${APP_DIR}/BankShared/BankShared.csproj BankApp/BankShared/
COPY ${APP_DIR}/BankAPI/BankAPI.csproj BankApp/BankAPI/
RUN dotnet restore BankApp/BankAPI/BankAPI.csproj

COPY ${APP_DIR}/BankShared/ BankApp/BankShared/
COPY ${APP_DIR}/BankAPI/ BankApp/BankAPI/
RUN dotnet publish BankApp/BankAPI/BankAPI.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:7083
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

EXPOSE 7083

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BankAPI.dll"]
