FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Jurassic.movieserviceapi/Jurassic.movieserviceapi.csproj Jurassic.movieserviceapi/
RUN dotnet restore Jurassic.movieserviceapi/Jurassic.movieserviceapi.csproj

COPY Jurassic.movieserviceapi/ Jurassic.movieserviceapi/
WORKDIR /src/Jurassic.movieserviceapi
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Jurassic.movieserviceapi.dll"]
