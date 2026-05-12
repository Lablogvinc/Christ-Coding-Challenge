# Use the official .NET 10.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Set the working directory
WORKDIR /src

# Copy the project file from the parent directory
COPY ../CCC_Backend.csproj .

# Copy the source code
COPY . .

# Build the application
RUN dotnet build "CCC_Backend.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "CCC_Backend.csproj" -c Release -o /app/publish

# Use the official .NET 10.0 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

# Set the working directory
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the backend port
EXPOSE 5000

# Set the entry point
ENTRYPOINT ["dotnet", "CCC_Backend.dll"]