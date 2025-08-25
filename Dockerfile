FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj files first for better layer caching
COPY SupportTicketSystem.Core/SupportTicketSystem.Core.csproj SupportTicketSystem.Core/
COPY SupportTicketSystem.Infrastructure/SupportTicketSystem.Infrastructure.csproj SupportTicketSystem.Infrastructure/
COPY SupportTicketSystem.API/SupportTicketSystem.API.csproj SupportTicketSystem.API/

# Restore packages
RUN dotnet restore "SupportTicketSystem.API/SupportTicketSystem.API.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR /src
RUN dotnet build "SupportTicketSystem.API/SupportTicketSystem.API.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "SupportTicketSystem.API/SupportTicketSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime stage
FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create directory for database
RUN mkdir -p /app/data

# Expose port
EXPOSE 5104

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5104
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SupportTicketSystem.API.dll"]
