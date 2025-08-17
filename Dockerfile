# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Set working directory to your API folder
WORKDIR "/src/Ecommerce Backend API"

# Restore
RUN dotnet restore "EcommerceBackendAPI.csproj"

# Use Directory.Build.props approach to handle warnings and errors
RUN echo "<Project><PropertyGroup><NoWarn>CS8600;CS8605;CS8625;CS0266</NoWarn><TreatWarningsAsErrors>false</TreatWarningsAsErrors></PropertyGroup></Project>" > Directory.Build.props && \
    dotnet publish "EcommerceBackendAPI.csproj" -c Release -o /app/publish -property:GeneratePackageOnBuild=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install necessary packages
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .


# Create uploads directory
RUN mkdir -p /app/uploads && chmod 755 /app/uploads

# Environment variables for Render.com
# ENV PORT=8080
# ENV ASPNETCORE_URLS=http://+:${PORT}

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# # RabbitMQ CloudAMQP settings
# ENV RabbitMQ__HostName=possum.lmq.cloudamqp.com
# ENV RabbitMQ__Username=pdnbjnjo
# ENV RabbitMQ__Password=c0GzGu_-51U_Fkzb7UnAsVvnz9JDwt8G
# ENV RabbitMQ__VirtualHost=pdnbjnjo
# ENV RabbitMQ__Ssl=true

# # RabbitMQ settings - using direct URI
# ENV RabbitMQ__Uri=amqps://pdnbjnjo:c0GzGu_-51U_Fkzb7UnAsVvnz9JDwt8G@possum.lmq.cloudamqp.com/pdnbjnjo
# ENV RabbitMQ__QueueName=ReserveStockQueue

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "EcommerceBackendAPI.dll"]