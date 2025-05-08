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
COPY --from=build /app/publish .

# Environment variables for Render.com
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}

# RabbitMQ CloudAMQP settings
ENV RabbitMQ__HostName=possum.lmq.cloudamqp.com
ENV RabbitMQ__Username=pdnbjnjo
ENV RabbitMQ__Password=c0GzGu_-51U_Fkzb7UnAsVvnz9JDwt8G
ENV RabbitMQ__VirtualHost=pdnbjnjo
ENV RabbitMQ__Ssl=true

# RabbitMQ settings - using direct URI
ENV RabbitMQ__Uri=amqps://pdnbjnjo:c0GzGu_-51U_Fkzb7UnAsVvnz9JDwt8G@possum.lmq.cloudamqp.com/pdnbjnjo
ENV RabbitMQ__QueueName=ReserveStockQueue

ENTRYPOINT ["dotnet", "EcommerceBackendAPI.dll"]