# Azure Container Instances code samples for documentation - .NET

This .NET Core 2.0 console application demonstrates several common [Azure Container Instances (ACI)](https://docs.microsoft.com/azure/container-instances/) operations in C# using the fluent APIs in the [Azure management libraries for .NET](https://docs.microsoft.com/dotnet/azure/dotnet-sdk-azure-concepts).

The code in this project is ingested into one or more articles on https://docs.microsoft.com.

## Features

The code in this sample project demontrates the following operations:

* Authenticate with Azure
* Create and delete resource group
* Create and delete single- and multi-container container groups
  * Exposes application container to internet with public DNS name
  * Multi-container group include both application and sidecar containers
* Get and print container group details
* Get and print container logs

## Getting Started

### Prerequisites

* [Microsoft .NET Core SDK](https://docs.microsoft.com/dotnet/core) version 2.1.2+

### Run the sample

1. Use the Azure CLI (or Cloud Shell) to generate an Azure credentials file:

   `az ad sp create-for-rbac --sdk-auth > my.azureauth`

1. Set environment variable `AZURE_AUTH_LOCATION` to the full path to the credentials file
1. `git clone https://github.com/Azure-Samples/aci-docs-sample-dotnet`
1. `cd aci-docs-sample-dotnet`
1. `dotnet run`

## Resources

* [Azure Management Libraries for .NET](https://github.com/Azure/azure-libraries-for-net) (GitHub)
