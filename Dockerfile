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

# Add environment variables for Render.com
# Define PORT with a default value if not provided at runtime
ARG PORT=8080
ENV PORT=${PORT}
ENV ASPNETCORE_URLS=http://+:${PORT}

ENTRYPOINT ["dotnet", "EcommerceBackendAPI.dll"]