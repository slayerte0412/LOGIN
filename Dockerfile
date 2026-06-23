FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copia todos los archivos al contenedor
COPY . ./

# Restaura y publica apuntando directamente a la carpeta del proyecto
RUN dotnet restore "LOGIN/LOGIN.csproj"
RUN dotnet publish "LOGIN/LOGIN.csproj" -c Release -o out

# Configura la imagen de ejecución usando .NET 10.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "LOGIN.dll"]
