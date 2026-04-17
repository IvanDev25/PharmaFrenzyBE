FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY IdentityApp.sln ./
COPY Api/Api.csproj Api/
RUN dotnet restore IdentityApp.sln

COPY Api/ Api/
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
