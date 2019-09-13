---
page_type: sample
languages:
- csharp
products:
- azure
description: "This .NET Core 2.0 console application demonstrates several common Azure Container Instances (ACI) operations in C# using the fluent APIs in the Azure Management Libraries for .NET."
urlFragment: aci-docs-sample-dotnet
---

# Azure Container Instances .NET code samples for documentation

This .NET Core 2.0 console application demonstrates several common [Azure Container Instances (ACI)](https://docs.microsoft.com/azure/container-instances/) operations in C# using the fluent APIs in the [Azure Management Libraries for .NET](https://docs.microsoft.com/dotnet/azure/dotnet-sdk-azure-concepts).

The code in this project is ingested into one or more articles on [docs.microsoft.com](https://docs.microsoft.com). Modifying the existing `#region` tags in the sample source may impact the rendering of code snippets on [docs.microsoft.com](https://docs.microsoft.com), and is discouraged.

## Features

The code in this sample project demonstrates the following operations:

* Authenticate with Azure
* Create resource group
* Create single- and multi-container container groups
  * Expose application container to internet with public DNS name
  * Multi-container group includes both application and sidecar containers
  * Demonstrate async container group create
  * Run a task-based container with custom command line and environment variables
* Get and print container group details
* Delete container groups
* Delete resource group

## Getting Started

### Prerequisites

* [Azure subscription](https://azure.microsoft.com/free)
* [Microsoft .NET Core SDK](https://docs.microsoft.com/dotnet/core) version 2.1.2+

### Run the sample

1. Use the Azure CLI (or Cloud Shell) to generate an Azure credentials file ([more info](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md))

   `az ad sp create-for-rbac --sdk-auth > my.azureauth`

1. Set environment variable `AZURE_AUTH_LOCATION` to the full path of the credentials file
1. `git clone https://github.com/Azure-Samples/aci-docs-sample-dotnet`
1. `cd aci-docs-sample-dotnet`
1. `dotnet run`

## Resources

* [Azure Management Libraries for .NET](https://github.com/Azure/azure-libraries-for-net) (GitHub)
* [More Azure Container Instances code samples](https://azure.microsoft.com/resources/samples/?sort=0&term=aci)
