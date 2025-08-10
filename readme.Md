InstantMart E-commerce Backend API v1.0

<img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-blue.svg">
<img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-15+-blue.svg">
<img alt="Redis" src="https://img.shields.io/badge/Redis-7.0+-red.svg">
<img alt="License" src="https://img.shields.io/badge/License-MIT-green.svg">
<img alt="Build Status" src="https://img.shields.io/badge/Build-Passing-brightgreen.svg">
A modern, scalable, and feature-rich e-commerce backend API built with .NET 8, implementing Clean Architecture principles and advanced design patterns for enterprise-grade applications.

🏗️ Architecture Overview
Clean Architecture Implementation
📁 InstantMart API
├── 🎯 Domain/              # Enterprise Business Rules
├── 🔧 Application/         # Application Business Rules  
├── 📊 Infrastructure/      # External Interfaces
├── 🌐 Ecommerce Backend API/ # Web API Layer
└── 🧪 tests/              # Testing Infrastructure

Design Patterns & Principles
-CQRS (Command Query Responsibility Segregation) - Separate read/write operations
-Repository Pattern - Data access abstraction
-Unit of Work Pattern - Manage database transactions
-Mediator Pattern - Decouple request handling (MediatR)
-Minimal API - Lightweight endpoint configuration
-Carter Framework - Organized API routing
-SOLID Principles - Maintainable and extensible code

🚀 Key Features
🛒 E-commerce Core
-Product Management - Categories, subcategories, inventory tracking
-Shopping Cart - Session-based cart with Redis caching
-Order Management - Complete order lifecycle with status tracking
-User Management - Authentication, profiles, addresses
-Wishlist - Save products for later
-Promo Codes - Discount management with usage tracking

💳 Payment Integration
Multiple Payment Gateways
-eSewa (Nepal's leading payment gateway)
-Khalti (Digital wallet integration)
-Cash on Delivery (COD)
-Payment Validation - Secure transaction verification

Billing System - Comprehensive invoice generation
🚚 Shipping & Logistics
-Dynamic Shipping Calculator - Distance-based pricing
-Multiple Shipping Options - Standard, express, free shipping
-Geographic Coverage - Service area management
-Delivery Tracking - Real-time status updates

📊 Advanced Features
-Real-time Notifications - SignalR hubs for admin and users
-Caching Strategy - Hybrid Redis + In-Memory caching
-Event-driven Architecture - Banner events and promotional campaigns
-Location Services - Google Maps integration for address validation
-Stock Management - Real-time inventory with reservation system

🛠️ Technology Stack
Backend Framework
-.NET 8 - Latest LTS version with enhanced performance
-ASP.NET Core Web API - High-performance web framework
-Carter - Minimal API routing and organization
-MediatR - CQRS implementation and request handling
-Database & Caching
-PostgreSQL 15+ - Primary database with advanced JSON support
-Entity Framework Core 8 - ORM with code-first migrations
-Redis 7.0+ - Distributed caching and session storage
-Hybrid Caching - Multi-level caching strategy

External Services
-Google Maps API - Location services and geocoding
-SignalR - Real-time communication
-JWT Authentication - Secure token-based auth
-FluentValidation - Input validation and sanitization
-DevOps & Infrastructure
-Docker - Containerization ready
-Nginx - Reverse proxy and load balancing
-Serilog - Structured logging
-Health Checks - Application monitoring

🔧 Prerequisites
-.NET 8 SDK
-PostgreSQL 15+
-Redis 7.0+
-Git
-Visual Studio 2022 / VS Code
⚡ Quick Start
1. Clone the Repository
git clone https://github.com/your-org/instantmart-backend-api.git
cd instantmart-backend-api

2. Environment Setup
Create .env file in the root directory:
# Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=instantmart_db
DB_USER=your_username
DB_PASSWORD=your_password

# Redis Configuration
REDIS_CONNECTION_STRING=localhost:6379

# JWT Configuration
JWT_SECRET=your-super-secret-jwt-key-here
JWT_ISSUER=InstantMart
JWT_AUDIENCE=InstantMart-Users

# Google Maps API
GOOGLE_MAPS_API_KEY=your_google_maps_api_key

# Payment Gateways
ESEWA_MERCHANT_ID=your_esewa_merchant_id
ESEWA_SECRET_KEY=your_esewa_secret
KHALTI_SECRET_KEY=your_khalti_secret

3. Database Setup
# Install Entity Framework tools
dotnet tool install --global dotnet-ef

# Update database with migrations
dotnet ef database update --project Infrastructure --startup-project "Ecommerce Backend API"

4. Install Dependencies
# Restore all NuGet packages
dotnet restore

# Build the solution
dotnet build

5. Run the Application
cd "Ecommerce Backend API"
dotnet run

The API will be available at:

HTTP: https://localhost:7028
Swagger UI: https://localhost:7028/swagger


📚 API Documentation
Core Endpoints
Authentication & Users
POST   /api/auth/register           # User registration
POST   /api/auth/login              # User login
POST   /api/auth/verify-otp         # OTP verification
POST   /api/auth/refresh-token      # Token refresh
GET    /api/users/profile           # User profile
PUT    /api/users/profile           # Update profile

Products & Catalog
GET    /api/products                # Get products (paginated)
GET    /api/products/{id}           # Get product details
GET    /api/categories              # Get categories
GET    /api/categories/{id}/products # Products by category

Shopping Cart
POST   /CartItem/create-cart        # Add item to cart
GET    /CartItem/getCartItemByUserId # Get user cart
PUT    /CartItem/update-quantity    # Update cart quantity
DELETE /CartItem/remove-cart-item   # Remove cart item

Orders & Checkout
POST   /api/orders/create           # Create order
GET    /api/orders/user/{userId}    # User orders
GET    /api/orders/{id}             # Order details
PUT    /api/orders/{id}/status      # Update order status

Payments
POST   /api/payments/esewa/initiate # eSewa payment
POST   /api/payments/khalti/verify  # Khalti verification
POST   /api/payments/cod/confirm    # COD confirmation

Advanced Features
Promo Codes & Discounts

POST   /api/promocodes/apply        # Apply promo code
GET    /api/promocodes/validate     # Validate promo code


Shipping & Logistics
POST   /api/shipping/calculate      # Calculate shipping cost
GET    /api/shipping/areas          # Service areas

🧪 Testing Strategy
Test Structure
📁 tests/
├── 🔬 UnitTests/           # Unit tests for business logic
├── 🔗 IntegrationTests/    # API integration tests
└── 📊 TestShared/          # Shared test utilities

Running Tests
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/UnitTests/
dotnet test tests/IntegrationTests/

Test Coverage
-Unit Tests - Business logic, services, repositories
-Integration Tests - End-to-end API testing
-Authentication Tests - JWT and OTP validation
-Payment Gateway Tests - Mock payment processing

🔧 Configuration
Application Settings
Key configuration sections in appsettings.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=instantmart;Username=user;Password=pass",
    "RedisConnection": "localhost:6379"
  },
  "JwtSettings": {
    "Secret": "your-jwt-secret",
    "ExpiryInMinutes": 60,
    "Issuer": "InstantMart",
    "Audience": "InstantMart-Users"
  },
  "GoogleMaps": {
    "ApiKey": "your-google-maps-key"
  },
  "PaymentGateways": {
    "Esewa": {
      "MerchantId": "your-merchant-id",
      "SecretKey": "your-secret"
    },
    "Khalti": {
      "SecretKey": "your-khalti-secret"
    }
  },
  "CacheSettings": {
    "DefaultTTLMinutes": 15,
    "ProductCacheTTLMinutes": 30,
    "UserCacheTTLMinutes": 10
  }
}

🚀 Performance Optimizations
Caching Strategy
-Hybrid Caching - L1 (In-Memory) + L2 (Redis)
-Product Caching - High-demand products cached for 30 minutes
-User Session Caching - Cart data cached for optimal performance
-Query Result Caching - Expensive database queries cached

Database Optimizations
-Entity Framework Optimizations - Proper includes, no-tracking queries
-Database Indexing - Optimized indexes for search and filtering
-Connection Pooling - Efficient database connection management
-Bulk Operations - Batch processing for large operations

API Performance
-Minimal API - Reduced overhead compared to traditional controllers
-Async/Await - Non-blocking operations throughout
-Response Compression - Gzip compression for API responses
-Request Validation - Early validation to prevent unnecessary processing

🔒 Security Features
-Authentication & Authorization
-JWT Tokens - Stateless authentication
-Role-based Access Control - Admin, User, Vendor roles
-OTP Verification - Two-factor authentication
-Password Hashing - BCrypt with salt

API Security
-CORS Configuration - Proper cross-origin resource sharing
-Rate Limiting - Prevent API abuse
-Input Validation - FluentValidation for all inputs
-SQL Injection Prevention - Parameterized queries via EF Core

Business Security
-Stock Reservation - Prevent overselling
-Payment Validation - Secure payment gateway integration
-Data Encryption - Sensitive data encryption at rest

🐳 Docker Deployment
Docker Compose Setup
version: '3.8'
services:
  api:
    build: .
    ports:
      - "8080:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - postgres
      - redis

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: instantmart
      POSTGRES_USER: user
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf

Building & Running

# Build and run with Docker Compose
docker-compose up --build

# Run in detached mode
docker-compose up -d

# Scale the API service
docker-compose up --scale api=3

📊 Monitoring & Logging
Structured Logging
-Serilog - Structured logging with multiple sinks
-Log Levels - Appropriate logging levels for different environments
-Request/Response Logging - Detailed API request tracking
-Performance Logging - Database query and API response times

Health Checks

GET /health              # Overall application health
GET /health/ready        # Readiness probe
GET /health/live         # Liveness probe


📄 API Versioning
The API uses URL versioning:

-v1: Current stable version (/api/v1/)
-v2: Future version (in development)

Backward Compatibility
-Breaking changes will introduce new versions
-Previous versions supported for 12 months
-Deprecation notices provided 6 months in advance

🆘 Troubleshooting
Common Issues
Database Connection Issues
# Check PostgreSQL service
sudo systemctl status postgresql

# Test connection
psql -h localhost -U username -d instantmart

Redis Connection Issues
# Check Redis service
redis-cli ping

# Should return: PONG

Migration Issues
# Reset database (development only)
dotnet ef database drop --project Infrastructure --startup-project "Ecommerce Backend API"
dotnet ef database update --project Infrastructure --startup-project "Ecommerce Backend API"

Performance Issues
-Check Redis cache hit rates - Monitor cache effectiveness
-Database query performance - Use SQL profiling tools
-Memory usage - Monitor application memory consumption

📞 Support & Contact
Issues: GitHub Issues
Discussions: GitHub Discussions
Email: support@instantmart.com
Dev-Mail : deuladipesh94@gmail.com
Documentation: API Documentation

📜 License
This project is licensed under the MIT License - see the LICENSE file for details.

🙏 Acknowledgments
-Microsoft - For the excellent .NET ecosystem
-PostgreSQL Team - For the robust database system
-Redis Team - For the high-performance caching solution
-Community Contributors - For their valuable feedback and contributions
-InstantMart Backend API v1.0 - Built with ❤️ for modern e-commerce solutions.

Last Updated: August 11 2025


