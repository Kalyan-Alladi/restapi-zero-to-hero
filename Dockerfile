# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DemoCiCdAzureApi.csproj", "./"]
RUN dotnet restore "DemoCiCdAzureApi.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "DemoCiCdAzureApi.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "DemoCiCdAzureApi.csproj" -c Release -o /app/publish
# Use the runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DemoCiCdAzureApi.dll"]