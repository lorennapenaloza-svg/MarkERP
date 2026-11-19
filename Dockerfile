# ── Compilación ──
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/MarkERP.Web/MarkERP.Web.csproj src/MarkERP.Web/
RUN dotnet restore src/MarkERP.Web/MarkERP.Web.csproj
COPY src/MarkERP.Web/ src/MarkERP.Web/
RUN dotnet publish src/MarkERP.Web/MarkERP.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── Ejecución ──
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MarkERP.Web.dll"]
