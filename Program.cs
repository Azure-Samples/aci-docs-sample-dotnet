namespace aci_doc_sample_dotnet
{
    using System;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ContainerInstance.Fluent;

    class Program
    {
        static void Main(string[] args)
        {
            // Set the AZURE_AUTH_LOCATION environment variable with the full
            // path to an auth file. Create an auth file with the Azure CLI:
            // az ad sp create-for-rbac --sdk-auth > my.azureauth
            string authFilePath = Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION");

            string resourceGroupName  = SdkContext.RandomResourceName("rg-aci-", 6);
            string containerGroupName = SdkContext.RandomResourceName("aci-", 6);
            string multiContainerGroupName = containerGroupName + "-multi";
            string containerImage1 = "microsoft/aci-helloworld";
            string containerImage2 = "microsoft/aci-tutorial-sidecar";

            // Authenticate with Azure
            IAzure azure = GetAzureContext(authFilePath);

            // Create a resource group in which the container groups are to be
            // created.
            CreateResourceGroup(azure, resourceGroupName, Region.USEast);

            // Demonstrate various container group operations
            CreateContainerGroup(azure, resourceGroupName, containerGroupName, containerImage1);
            CreateContainerGroupMulti(azure, resourceGroupName, multiContainerGroupName, containerImage1, containerImage2);
            ListContainerGroups(azure, resourceGroupName);
            PrintContainerGroupDetails(azure, resourceGroupName, containerGroupName);

            // Clean up container groups
            DeleteContainerGroup(azure, resourceGroupName, containerGroupName);
            DeleteContainerGroup(azure, resourceGroupName, multiContainerGroupName);

            // Remove resource group (if the user so chooses)
            Console.WriteLine();
            Console.Write($"Delete resource group '{resourceGroupName}'? [yes] no: ");
            string response = Console.ReadLine().Trim().ToLower();
            if (response != "n" && response != "no")
            {
                DeleteResourceGroup(azure, resourceGroupName);
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        /// <summary>
        /// Returns an authenticated Azure context using the credentials in the
        /// specified auth file.
        /// </summary>
        private static IAzure GetAzureContext(string authFilePath)
        {            
            IAzure azure;
            ISubscription sub;

            try
            {
                Console.WriteLine($"Authenticating with Azure using credentials in file at {authFilePath}");

                azure = Azure.Authenticate(authFilePath).WithDefaultSubscription();
                sub = azure.GetCurrentSubscription();

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

            return azure;
        }

        /// <summary>
        /// Creates a resource group of the specified name.
        /// </summary>
        private static void CreateResourceGroup(IAzure azure, string resourceGroupName, Region azureRegion)
        {
            Console.WriteLine($"\nCreating resource group '{resourceGroupName}'...");

            azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(azureRegion)
                .Create();
        }

        /// <summary>
        /// Creates a container group with a single container in the specified resource group.
        /// </summary>
        private static void CreateContainerGroup(IAzure azure,
                                                 string resourceGroupName, 
                                                 string containerGroupName, 
                                                 string containerImage)
        {
            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            // Create the container group
            var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(containerImage)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(1.0)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

            Console.WriteLine($"Container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }

        /// <summary>
        /// Creates a container group with two containers in the specified resource group.
        /// </summary>
        private static void CreateContainerGroupMulti(IAzure azure,
                                                      string resourceGroupName,
                                                      string containerGroupName, 
                                                      string containerImage1, 
                                                      string containerImage2)
        {
            Console.WriteLine($"\nCreating multi-container container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            // Create the container group
            var containerGroup = azure.ContainerGroups.Define(containerGroupName)
                .WithRegion(azureRegion)
                .WithExistingResourceGroup(resourceGroupName)
                .WithLinux()
                .WithPublicImageRegistryOnly()
                .WithoutVolume()
                .DefineContainerInstance(containerGroupName + "-1")
                    .WithImage(containerImage1)
                    .WithExternalTcpPort(80)
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .DefineContainerInstance(containerGroupName + "-2")
                    .WithImage(containerImage2)
                    .WithoutPorts()
                    .WithCpuCoreCount(0.5)
                    .WithMemorySizeInGB(1)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .Create();

            Console.WriteLine($"Container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }

        /// <summary>
        /// Prints the container groups in the specified resource group.
        /// </summary>
        private static void ListContainerGroups(IAzure azure, string resourceGroupName)
        {
            Console.WriteLine($"\nListing container groups in resource group '{resourceGroupName}'...");

            foreach (var containerGroup in azure.ContainerGroups.ListByResourceGroup(resourceGroupName))
            {
                Console.WriteLine($"{containerGroup.Name}");
            }
        }

        /// <summary>
        /// Gets the specified container group and then prints a few of its
        /// properties and their values.
        /// </summary>
        private static void PrintContainerGroupDetails(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            Console.Write($"\nGetting container group details for container group '{containerGroupName}'...");

            IContainerGroup containerGroup = null;
            while (containerGroup == null)
            {
                Console.Write(".");

                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

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
        /// Deletes the specified container group.
        /// </summary>
        private static void DeleteContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            Console.WriteLine($"\nPress ENTER to delete container group '{containerGroupName}':");
            Console.ReadLine();

            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine($"Deleting container group '{containerGroupName}'...");

            azure.ContainerGroups.DeleteById(containerGroup.Id);
        }

        /// <summary>
        /// Deletes the specified resource group.
        /// </summary>
        private static void DeleteResourceGroup(IAzure azure, string resourceGroupName)
        {
            Console.WriteLine($"\nDeleting resource group '{resourceGroupName}'...");

            azure.ResourceGroups.DeleteByName(resourceGroupName);
        }
    }
}
