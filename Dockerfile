```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LOGIN.sln", "./"]
COPY ["LOGIN/LOGIN.csproj", "LOGIN/"]
RUN dotnet restore
COPY . .
WORKDIR "/src/LOGIN"
RUN dotnet build "LOGIN.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LOGIN.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LOGIN.dll"]
