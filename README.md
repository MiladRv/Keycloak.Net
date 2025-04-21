# Keycloak.Net.Sdk

> 🧰 A powerful and modular .NET SDK for integrating with [Keycloak](https://www.keycloak.org/)

This SDK is designed to provide a clean, extensible, and testable interface to interact with Keycloak in .NET applications using `HttpClientFactory`, typed services, and native DTOs.

📦 NuGet: [Keycloak.Net.Sdk](https://www.nuget.org/packages/Keycloak.Net.Sdk)

---

## ✨ Features

- Sign up / Sign in users  
- Manage users (set password, enable/disable, delete)  
- Manage roles (realm & client)  
- Assign / remove roles to users  
- Manage realms and clients  
- Token-based authentication  
- Built-in retry policy & auth handler  
- Supports `HttpClientFactory` and dependency injection  

---


### Prerequisites

Before using the **Keycloak.Net** SDK, ensure you have the following:

- A **Keycloak** server running and accessible.
- **.NET 8+** project setup.

## ⚙️ Installation

```bash
dotnet add package Keycloak.Net.Sdk
```

## 🔧 Configuration 

1- Add this config to your appsettings.json

```
  "keycloak": {
    "ServerUrl": "keycloak service address",
    "RealmName": "your realm name",
    "ClientId": "your clientId",
    "ClientSecret":"your clientSecret",
    "AdminUsername": "master realm username",
    "AdminPassword": "master realm password",
    "NumberOfRetries": 3,
    "DelayBetweenRetryRequestsInSeconds": 1
  }
```
2- Register to DI
```
builder.Services.AddKeycloak(builder.Configuration);
```

### License
This project is licensed under the MIT License - see the LICENSE file for details.

### Contact
For more information, questions, or feedback, you can reach out to me at miladrivandi73@gmail.com or open an issue in this repository.


