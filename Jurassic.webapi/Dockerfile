FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Jurassic.webapi/Jurassic.webapi.csproj Jurassic.webapi/
RUN dotnet restore Jurassic.webapi/Jurassic.webapi.csproj

COPY Jurassic.webapi/ Jurassic.webapi/
WORKDIR /src/Jurassic.webapi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Workaround (.NET 10): ensure Blazor framework boot scripts are present under wwwroot/_framework.
# Some publish/Docker configurations can omit these runtime static assets, which prevents @onclick
# and other interactive behaviors from working in the browser.
RUN bash -lc "set -euo pipefail; \
  if [ ! -f /app/publish/wwwroot/_framework/blazor.web.js ]; then \
    echo 'blazor.web.js missing; generating _framework assets...'; \
    mkdir -p /tmp/_fw && cd /tmp/_fw; \
    dotnet new blazor -n _FwApp >/dev/null; \
    dotnet publish -c Release -o /tmp/_fw_out /tmp/_fw/_FwApp >/dev/null; \
    mkdir -p /app/publish/wwwroot/_framework; \
    cp -a /tmp/_fw_out/wwwroot/_framework/. /app/publish/wwwroot/_framework/; \
  fi"

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Jurassic.webapi.dll"]
