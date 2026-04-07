FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Jurassic.webapi/Jurassic.webapi.csproj Jurassic.webapi/
RUN dotnet restore Jurassic.webapi/Jurassic.webapi.csproj

COPY Jurassic.webapi/ Jurassic.webapi/
WORKDIR /src/Jurassic.webapi
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Jurassic.webapi.dll"]
