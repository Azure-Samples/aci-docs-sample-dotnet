namespace aci_doc_sample_dotnet
{
    using System;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ContainerInstance.Fluent;

    class Program
    {
        // Set the AZURE_AUTH_LOCATION environment variable with the full path
        // to an auth file. You can create an auth file with the Azure CLI:
        // az ad sp create-for-rbac --sdk-auth > my.azureauth
        private static string AuthFilePath = Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION");

        private static string ResourceGroupName  = SdkContext.RandomResourceName("rg-aci-", 6);
        private static string ContainerGroupName = SdkContext.RandomResourceName("aci-", 6);
        private static string MultiContainerGroupName = ContainerGroupName + "-multi";
        private static string ContainerImage1 = "microsoft/aci-helloworld";
        private static string ContainerImage2 = "microsoft/aci-tutorial-sidecar";

        private static IAzure MyAzure;
        private static readonly Region AzureRegion = Region.USEast;

        static void Main(string[] args)
        {
            // Authenticate with Azure and create a resource group
            Authenticate(AuthFilePath);
            CreateResourceGroup(ResourceGroupName);

            // Demonstrate various container group operations
            CreateContainerGroup(ContainerGroupName, ContainerImage1);
            CreateContainerGroupMulti(MultiContainerGroupName, ContainerImage1, ContainerImage2);
            ListContainerGroups(ResourceGroupName);
            PrintContainerGroupDetails(ContainerGroupName);
            PrintContainerLogs(ContainerGroupName);

            // Clean up container groups
            DeleteContainerGroup(ContainerGroupName);
            DeleteContainerGroup(MultiContainerGroupName);

            // Remove resource group (if the user so chooses)
            Console.WriteLine();
            Console.Write($"Delete resource group '{ResourceGroupName}'? [yes] no: ");
            string response = Console.ReadLine().Trim().ToLower();
            if (response != "n" && response != "no")
            {
                DeleteResourceGroup(ResourceGroupName);
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static void Authenticate(string authFilePath)
        {            
            ISubscription sub;

            try
            {
                Console.WriteLine($"Authenticating with Azure using credentials in file at {authFilePath}");

                MyAzure = Azure.Authenticate(authFilePath).WithDefaultSubscription();
                sub = MyAzure.GetCurrentSubscription();

                Console.WriteLine($"Authenticated with subscription '{sub.DisplayName}' (ID: {sub.SubscriptionId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFailed to authenticate:\n{ex.Message}");

                if (String.IsNullOrEmpty(authFilePath))
                {
                    Console.WriteLine("Have you set the AZURE_AUTH_LOCATION environment variable?");
                }

                throw;
            }
        }

        private static void CreateResourceGroup(string resourceGroupName)
        {
            Console.WriteLine($"\nCreating resource group '{resourceGroupName}'...");

            MyAzure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(AzureRegion)
                .Create();
        }

        private static void CreateContainerGroup(string containerGroupName, string containerImageName)
        {
            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            var containerGroup = MyAzure.ContainerGroups.Define(containerGroupName)
                .WithRegion(AzureRegion)
                .WithExistingResourceGroup(ResourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(ContainerImage1)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(1.0)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

            Console.WriteLine($"Container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }

        private static void CreateContainerGroupMulti(string containerGroupName, string containerImageName1, string containerImageName2)
        {
            Console.WriteLine($"\nCreating multi-container container group '{containerGroupName}'...");

            var containerGroup = MyAzure.ContainerGroups.Define(containerGroupName)
                .WithRegion(AzureRegion)
                .WithExistingResourceGroup(ResourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(ContainerImage1)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .DefineContainerInstance(containerGroupName + "-2")
                    .WithImage(ContainerImage2)
                    .WithoutPorts()
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

            Console.WriteLine($"Container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }

        private static void ListContainerGroups(string resourceGroupName)
        {
            Console.WriteLine($"\nListing container groups in resource group '{resourceGroupName}'...");

            foreach (var containerGroup in MyAzure.ContainerGroups.ListByResourceGroup(resourceGroupName))
            {
                Console.WriteLine($"{containerGroup.Name}");
            }
        }

        /// <summary>
        /// Gets and then prints several properties and their values for the specified container group.
        /// </summary>
        private static void PrintContainerGroupDetails(string containerGroupName)
        {
            Console.Write($"\nGetting container group details for container group '{containerGroupName}'...");

            IContainerGroup containerGroup = null;
            while (containerGroup == null)
            {
                Console.Write(".");

                containerGroup = MyAzure.ContainerGroups.GetByResourceGroup(ResourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine();
            Console.WriteLine(containerGroup.Name);
            Console.WriteLine("--------------------------------");
            Console.WriteLine($"State:  {containerGroup.State}");
            Console.WriteLine($"FQDN:   {containerGroup.Fqdn}");
            Console.WriteLine($"IP:     {containerGroup.IPAddress}");
            Console.WriteLine($"Region: {containerGroup.RegionName}");
        }

        /// <summary>
        /// Prints the logs of the first container found in the specified container group.
        /// </summary>
        private static void PrintContainerLogs(string containerGroupName)
        {
            
        }

        /// <summary>
        /// Deletes the specified container group.
        /// </summary>
        private static void DeleteContainerGroup(string containerGroupName)
        {
            Console.WriteLine($"\nPress ENTER to delete container group '{containerGroupName}':");
            Console.ReadLine();

            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = MyAzure.ContainerGroups.GetByResourceGroup(ResourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine($"Deleting container group '{containerGroupName}'...");
            
            MyAzure.ContainerGroups.DeleteById(containerGroup.Id);
        }

        /// <summary>
        /// Deletes the specified resource group.
        /// </summary>
        private static void DeleteResourceGroup(string resourceGroupName)
        {
            Console.WriteLine($"\nDeleting resource group '{resourceGroupName}'...");

            MyAzure.ResourceGroups.DeleteByName(resourceGroupName);
        }
    }
}