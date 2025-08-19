
# InstantMart E-commerce Backend API v1.0

<img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-blue.svg">  
<img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-15+-blue.svg">  
<img alt="Redis" src="https://img.shields.io/badge/Redis-7.0+-red.svg">  
<img alt="License" src="https://img.shields.io/badge/License-MIT-green.svg">  
<img alt="Build Status" src="https://img.shields.io/badge/Build-Passing-brightgreen.svg">  

A modern, scalable, and feature-rich e-commerce backend API built with **.NET 8**, implementing **Clean Architecture** and enterprise-grade design patterns.

## 🏗️ Architecture Overview

**Clean Architecture Layers**

📁 InstantMart API
├── 🎯 Domain/              # Enterprise Business Rules
├── 🔧 Application/         # Application Business Rules  
├── 📊 Infrastructure/      # External Interfaces
├── 🌐 Ecommerce Backend API/ # Web API Layer
└── 🧪 tests/              # Testing Infrastructure


**Design Patterns & Principles**

* **CQRS** – Separate read/write operations
* **Repository Pattern** – Data access abstraction
* **Unit of Work** – Transaction management
* **Mediator (MediatR)** – Request/response handling
* **Carter Framework** – Minimal & modular API routing
* **SOLID Principles** – Maintainability & extensibility



## 🚀 Key Features

### 🛒 E-commerce Core

* Product, Category & Inventory Management
* Shopping Cart (Redis-based)
* Order Lifecycle Management
* User Authentication & Profiles
* Wishlist & Promo Codes

### 💳 Payment Integration

* eSewa, Khalti, COD
* Secure payment validation & invoices

### 🚚 Shipping & Logistics

* Distance-based shipping cost calculator
* Multiple shipping options
* Real-time delivery tracking

### 📊 Advanced Features

* Real-time Notifications (SignalR)
* Hybrid Caching (Redis + In-Memory)
* Location Services (Google Maps)
* Stock Reservation & Management


## 🛠️ Technology Stack

* **.NET 8** (ASP.NET Core Web API, Minimal API, Carter, MediatR)
* **PostgreSQL 15+** (Primary DB with EF Core 8)
* **Redis 7.0+** (Caching & Sessions)
* **Google Maps API** (Address validation & geocoding)
* **SignalR** (Real-time communication)
* **JWT Authentication** (Token-based security)
* **FluentValidation** (Input validation)
* **Serilog** (Structured logging & monitoring)



## 🔧 Prerequisites

* .NET 8 SDK
* PostgreSQL 15+
* Redis 7.0+
* Git
* Visual Studio 2022 / VS Code



## ⚡ Quick Start

### 1. Clone Repository

git clone https://github.com/your-org/instantmart-backend-api.git
cd instantmart-backend-api


### 2. Configure Environment

Create a `.env` file in the root:

env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=instantmart_db
DB_USER=your_username
DB_PASSWORD=your_password

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# JWT
JWT_SECRET=your-super-secret-jwt-key-here
JWT_ISSUER=InstantMart
JWT_AUDIENCE=InstantMart-Users

# Google Maps
GOOGLE_MAPS_API_KEY=your_google_maps_api_key

# Payment Gateways
ESEWA_MERCHANT_ID=your_esewa_id
ESEWA_SECRET_KEY=your_esewa_secret
KHALTI_SECRET_KEY=your_khalti_secret


### 3. Database Setup


dotnet tool install --global dotnet-ef
dotnet ef database update --project Infrastructure --startup-project "Ecommerce Backend API"


### 4. Build & Run


dotnet restore
dotnet build
cd "Ecommerce Backend API"
dotnet run


API will be available at:

* **HTTP**: `https://localhost:7028`
* **Swagger UI**: `https://localhost:7028/swagger`



## 📚 API Documentation

### Authentication & Users

* `POST /api/auth/register` – Register
* `POST /api/auth/login` – Login
* `POST /api/auth/verify-otp` – Verify OTP
* `POST /api/auth/refresh-token` – Refresh token
* `GET /api/users/profile` – Get profile
* `PUT /api/users/profile` – Update profile

### Products & Catalog

* `GET /api/products` – List products
* `GET /api/products/{id}` – Product details
* `GET /api/categories` – Get categories
* `GET /api/categories/{id}/products` – Products by category

### Cart

* `POST /CartItem/create-cart` – Add to cart
* `GET /CartItem/getCartItemByUserId` – Get user cart
* `PUT /CartItem/update-quantity` – Update cart
* `DELETE /CartItem/remove-cart-item` – Remove item

### Orders & Payments

* `POST /api/orders/create` – Create order
* `GET /api/orders/user/{userId}` – Get user orders
* `POST /api/payments/esewa/initiate` – eSewa payment
* `POST /api/payments/khalti/verify` – Khalti verification
* `POST /api/payments/cod/confirm` – COD confirmation



## 🧪 Testing


dotnet test
dotnet test --collect:"XPlat Code Coverage"


* **Unit Tests** – Business logic & services
* **Integration Tests** – End-to-end API
* **Authentication & Payment Tests**



## 🚀 Performance Optimizations

* **Hybrid Caching** – Redis + In-Memory
* **EF Core Optimizations** – No-tracking queries, batch operations
* **Database Indexing** – Optimized for search & filtering
* **Compression & Async APIs** – Faster response times



## 🔒 Security

* JWT Tokens & Role-based Access Control
* OTP Verification (2FA)
* BCrypt Password Hashing
* Input Validation (FluentValidation)
* Rate Limiting & SQL Injection Prevention



## 📞 Support & Contact

* Issues: GitHub Issues
* Email: [support@instantmart.com](mailto:support@instantmart.com)
* Dev-Mail: [deuladipesh94@gmail.com](mailto:deuladipesh94@gmail.com)


