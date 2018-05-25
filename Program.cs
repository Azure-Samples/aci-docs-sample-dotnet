namespace aci_doc_sample_dotnet
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Management.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent;
    using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
    using Microsoft.Azure.Management.ContainerInstance.Fluent;
    using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;

    class Program
    {
        static void Main(string[] args)
        {
            #region local_config
            string resourceGroupName  = SdkContext.RandomResourceName("rg-aci-", 6);
            
            string containerGroupName = SdkContext.RandomResourceName("aci-", 6);
            string multiContainerGroupName = containerGroupName + "-multi";
            string asyncContainerGroupName = containerGroupName + "-async";
            string taskContainerGroupName  = containerGroupName + "-task";
            
            string containerImageApp     = "microsoft/aci-helloworld";
            string containerImageSidecar = "microsoft/aci-tutorial-sidecar";
            string taskContainerImage    = "microsoft/aci-wordcount";
            
            // Set the AZURE_AUTH_LOCATION environment variable with the full
            // path to an auth file. Create an auth file with the Azure CLI:
            // az ad sp create-for-rbac --sdk-auth > my.azureauth
            string authFilePath = Environment.GetEnvironmentVariable("AZURE_AUTH_LOCATION");

            // Authenticate with Azure
            IAzure azure = GetAzureContext(authFilePath);
            #endregion

            // Create a resource group in which the container groups are to be
            // created.
            CreateResourceGroup(azure, resourceGroupName, Region.USEast);

            // Demonstrate various container group operations
            CreateContainerGroup(azure, resourceGroupName, containerGroupName, containerImageApp);
            CreateContainerGroupMulti(azure, resourceGroupName, multiContainerGroupName, containerImageApp, containerImageSidecar);
            CreateContainerGroupWithPolling(azure, resourceGroupName, asyncContainerGroupName, containerImageApp);
            RunTaskBasedContainer(azure, resourceGroupName, taskContainerGroupName, taskContainerImage, null);
            ListContainerGroups(azure, resourceGroupName);
            PrintContainerGroupDetails(azure, resourceGroupName, containerGroupName);

            // Clean up container groups
            Console.WriteLine($"\nPress ENTER to delete all container groups...");
            Console.ReadLine();
            DeleteContainerGroup(azure, resourceGroupName, containerGroupName);
            DeleteContainerGroup(azure, resourceGroupName, multiContainerGroupName);
            DeleteContainerGroup(azure, resourceGroupName, asyncContainerGroupName);
            DeleteContainerGroup(azure, resourceGroupName, taskContainerGroupName);

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

        #region azure_auth
        /// <summary>
        /// Returns an authenticated Azure context using the credentials in the
        /// specified auth file.
        /// </summary>
        /// <param name="authFilePath">The full path to a credentials file on the local filesystem.</param>
        /// <returns>Authenticated IAzure context.</returns>
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
        #endregion

        #region create_resource_group
        /// <summary>
        /// Creates a resource group of the specified name.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group to be created.</param>
        /// <param name="azureRegion">The Region in which to create the resource group.</param>
        private static void CreateResourceGroup(IAzure azure, string resourceGroupName, Region azureRegion)
        {
            Console.WriteLine($"\nCreating resource group '{resourceGroupName}'...");

            azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(azureRegion)
                .Create();
        }
        #endregion

        #region create_container_group
        /// <summary>
        /// Creates a container group with a single container.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage">The container image name and tag, for example 'microsoft\aci-helloworld:latest'.</param>
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

            Console.WriteLine($"Once DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }
        #endregion

        #region create_container_group_multi
        /// <summary>
        /// Creates a container group with two containers in the specified resource group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage1">The first container image name and tag, for example 'microsoft\aci-helloworld:latest'.</param>
        /// <param name="containerImage2">The second container image name and tag, for example 'microsoft\aci-tutorial-sidecar:latest'.</param>
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

            Console.WriteLine($"Once DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }
        #endregion

        #region create_container_group_polling
        /// <summary>
        /// Creates a container group with a single container asynchronously, and
        /// polls its status until its state is 'Running'.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage">The container image name and tag, for example 'microsoft\aci-helloworld:latest'.</param>
        private static void CreateContainerGroupWithPolling(IAzure azure,
                                                 string resourceGroupName, 
                                                 string containerGroupName, 
                                                 string containerImage)
        {
            Console.WriteLine($"\nCreating container group '{containerGroupName}'...");

            // Get the resource group's region
            IResourceGroup resGroup = azure.ResourceGroups.GetByName(resourceGroupName);
            Region azureRegion = resGroup.Region;

            // Create the container group using a fire-and-forget task
            Task.Run(() =>

                azure.ContainerGroups.Define(containerGroupName)
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
                    .CreateAsync()
            );

            // Poll for the container group
            IContainerGroup containerGroup = null;
            while(containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

                Console.Write(".");

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine();

            // Poll until the container group is running
            while(containerGroup.State != "Running")
            {
                Console.WriteLine($"Container group state: {containerGroup.Refresh().State}");
                
                Thread.Sleep(1000);
            }

            Console.WriteLine($"\nOnce DNS has propagated, container group '{containerGroup.Name}' will be reachable at http://{containerGroup.Fqdn}");
        }
        #endregion

        #region create_container_group_task
        /// <summary>
        /// Creates a container group with a single task-based container who's
        /// restart policy is 'Never'. If specified, the container runs a custom
        /// command line at startup.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group in which to create the container group.</param>
        /// <param name="containerGroupName">The name of the container group to create.</param>
        /// <param name="containerImage">The container image name and tag, for example 'microsoft\aci-wordcount:latest'.</param>
        /// <param name="startCommandLine">The command line that should be executed when the container starts. This value can be <c>null</c>.</param>
        private static void RunTaskBasedContainer(IAzure azure,
                                                 string resourceGroupName, 
                                                 string containerGroupName, 
                                                 string containerImage,
                                                 string startCommandLine)
        {
            // If a start command wasn't specified, use a default
            if (String.IsNullOrEmpty(startCommandLine))
            {
                startCommandLine = "python wordcount.py http://shakespeare.mit.edu/romeo_juliet/full.html";
            }

            // Configure some environment variables in the container which the
            // wordcount.py or other script can read to modify its behavior.
            Dictionary<string, string> envVars = new Dictionary<string, string>
            {
                { "NumWords", "5" },
                { "MinLength", "8" }
            };

            Console.WriteLine($"\nCreating container group '{containerGroupName}' with start command '{startCommandLine}'");

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
                    .WithStartingCommandLines(startCommandLine.Split())
                    .WithEnvironmentVariables(envVars)
                    .Attach()
                .WithDnsPrefix(containerGroupName)
                .WithRestartPolicy(ContainerGroupRestartPolicy.Never)
                .Create();

            // Print the container's logs
            Console.WriteLine($"Logs for container '{containerGroupName}-1':");
            Console.WriteLine(containerGroup.GetLogContent(containerGroupName + "-1"));
        }
        #endregion

        #region list_container_groups
        /// <summary>
        /// Prints the container groups in the specified resource group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group(s).</param>
        private static void ListContainerGroups(IAzure azure, string resourceGroupName)
        {
            Console.WriteLine($"Listing container groups in resource group '{resourceGroupName}'...");

            foreach (var containerGroup in azure.ContainerGroups.ListByResourceGroup(resourceGroupName))
            {
                Console.WriteLine($"{containerGroup.Name}");
            }
        }
        #endregion

        #region get_container_group
        /// <summary>
        /// Gets the specified container group and then prints a few of its properties and their values.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group.</param>
        /// <param name="containerGroupName">The name of the container group whose details should be printed.</param>
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
        #endregion

        #region delete_container_group
        /// <summary>
        /// Deletes the specified container group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the container group.</param>
        /// <param name="containerGroupName">The name of the container group to delete.</param>
        private static void DeleteContainerGroup(IAzure azure, string resourceGroupName, string containerGroupName)
        {
            IContainerGroup containerGroup = null;

            while (containerGroup == null)
            {
                containerGroup = azure.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

                SdkContext.DelayProvider.Delay(1000);
            }

            Console.WriteLine($"Deleting container group '{containerGroupName}'...");

            azure.ContainerGroups.DeleteById(containerGroup.Id);
        }
        #endregion
        
        #region delete_resource_group
        /// <summary>
        /// Deletes the specified resource group.
        /// </summary>
        /// <param name="azure">An authenticated IAzure object.</param>
        /// <param name="resourceGroupName">The name of the resource group to delete.</param>
        private static void DeleteResourceGroup(IAzure azure, string resourceGroupName)
        {
            Console.WriteLine($"\nDeleting resource group '{resourceGroupName}'...");

            azure.ResourceGroups.DeleteByName(resourceGroupName);
        }
        #endregion
    }
}
