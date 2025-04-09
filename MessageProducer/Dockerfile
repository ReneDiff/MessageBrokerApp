# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers to leverage Docker cache
COPY MessageProducer/*.csproj ./MessageProducer/
COPY MessageShared/*.csproj ./MessageShared/
# Copy global.json if it influences restore
COPY global.json ./
RUN dotnet restore ./MessageProducer/MessageProducer.csproj

# Copy the rest of the source code
COPY MessageProducer/. ./MessageProducer/
COPY MessageShared/. ./MessageShared/

# Publish the application
WORKDIR /source/MessageProducer
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Set the entry point for the container
ENTRYPOINT ["dotnet", "MessageProducer.dll"]