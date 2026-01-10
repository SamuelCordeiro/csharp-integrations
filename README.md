# C# Integrations
Reusable .NET integrations, middlewares, and authentication building blocks for modern APIs.

## Overview

This repository is a **collection of generic and reusable C# building blocks** designed to accelerate the development of APIs and backend services.

It centralizes **integrations, middlewares, and cross-cutting concerns**, promoting **consistency, scalability, and code reuse** across modern .NET applications.

The project follows a **modular architecture**, allowing each component to be used independently or composed into larger solutions.


## Generic Swagger Middleware

This project provides a **generic Swagger (OpenAPI) implementation** exposed as a reusable middleware.

It abstracts and standardizes Swagger configuration, reducing repetitive setup and ensuring consistent API documentation across multiple services.

### Purpose

- Eliminate duplicated Swagger configuration
- Enforce consistent API documentation standards
- Simplify maintenance and future evolution

## API Authentication with Bearer Token

This project includes a **generic API authentication implementation using JWT Bearer Tokens**, suitable for secure and scalable APIs.

### Features

- JWT token generation
- Centralized authentication and authorization configuration
- Claims-based identity handling
- Token expiration and validation
- Easy integration with protected endpoints
- Fully compatible with ASP.NET Core authentication pipeline

## Why this project exists

In many .NET projects, common concerns such as Swagger configuration and authentication are repeatedly reimplemented.

This repository aims to provide **ready-to-use, well-structured, and extensible solutions** that can be easily shared across projects and teams.

## Project Structure

The solution is organized into two main projects, clearly separating **integration usage and demonstration** from **shared middleware and core logic**.

### csharp-integrations-core

A reusable core library that centralizes middlewares, integrations, and shared resources, functioning as a business and infrastructure layer.

```text
csharp-integrations-core/
 ├─ Auth/
 │  └─ Bearer/
 │     └─ BearerMiddleware.cs
 ├─ Swagger/
 │  └─ SwaggerMiddleware.cs
 └─ GlobalResources/
    ├─ Models/
    │  └─ User.cs
    └─ Repositories/
       └─ UserRepository.cs
```

### csharp-integrations-api

The API project acts as a **consumer and showcase** for the integrations provided by the core library.

Its main purpose is to demonstrate **how easily the shared integrations and middlewares can be plugged into a real API**, serving both as a usage example and a functional entry point.

```text
csharp-integrations-api/
 └─ Controllers/
    └─ Auth/
       └─ Bearer/
          └─ AuthBearerController.cs
```

