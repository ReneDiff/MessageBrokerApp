# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore as distinct layers
COPY MessageConsumer/*.csproj ./MessageConsumer/
COPY MessageShared/*.csproj ./MessageShared/
COPY global.json ./
RUN dotnet restore ./MessageConsumer/MessageConsumer.csproj

# Copy the rest of the source code
COPY MessageConsumer/. ./MessageConsumer/
COPY MessageShared/. ./MessageShared/

# Publish the application
WORKDIR /source/MessageConsumer
RUN dotnet publish -c Release -o /app --no-restore

# Stage 2: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app ./

# Set the entry point for the container
ENTRYPOINT ["dotnet", "MessageConsumer.dll"]