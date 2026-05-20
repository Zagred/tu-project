FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
ARG APP_DIR=BankApp

COPY ${APP_DIR}/BankAPP.Shared/BankAPP.Shared.csproj BankApp/BankAPP.Shared/
COPY ${APP_DIR}/BankWeb/BankWeb.csproj BankApp/BankWeb/
RUN dotnet restore BankApp/BankWeb/BankWeb.csproj

COPY ${APP_DIR}/BankAPP.Shared/ BankApp/BankAPP.Shared/
COPY ${APP_DIR}/BankWeb/ BankApp/BankWeb/
RUN dotnet publish BankApp/BankWeb/BankWeb.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENV DOTNET_PRINT_TELEMETRY_MESSAGE=false

EXPOSE 5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BankWeb.dll"]
