using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using dotnet_roslyn_dynamic_api.Models;

namespace dotnet_roslyn_dynamic_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private Storage<Entity> _storage;

        public EntityController(Storage<Entity> storage)
        {
            _storage = storage;
            // Seed initial entities

            if (_storage.GetAll().Count() == 0)
            foreach (string className in GlobalVariables.GeneratedClasses)
            {
                Entity _ent = new Entity
                {
                    Id = Guid.NewGuid(),
                    Name = className,
                    Fields = string.Empty,
                    isActive = true
                };
                _storage.Add(_ent.Id, _ent);
            }
        }

        [HttpGet]
        public IEnumerable<Entity> Get()
        {
            return _storage.GetAll();
        }

        [HttpGet("{id}")]
        public Entity Get(Guid id)
        {
            return _storage.GetById(id);
        }

        [HttpPost("{id}")]
        public void Post(Guid id, [FromServices] ApplicationPartManager partManager, [FromServices]DynamicActionDescriptorChangeProvider provider, [FromBody]Entity value)
        {
            _storage.Add(id, value);
            // Create Controller - Method Below
            CreateController(value.Name, value.Fields);
            // Triggers the invalidation of the cached collection of action descriptors
            provider.HasChanged = true;
            provider.TokenSource.Cancel();
        }

        private void CreateController(string name, string fields)
        {
            // Insert code to append the fields value to the Azure BLOB file
            // Setup and retrieve basic job settings
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("config/appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            AzureBlobSettings azureBlobSettings = config.GetRequiredSection("AzureBlob").Get<AzureBlobSettings>();

            // Download file to data directory -- functions locally and on docker
            string connectionString = azureBlobSettings.ConnectionString;
            BlobServiceClient serviceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient("public");
            string localPath = "./Data/";
            string fileName = azureBlobSettings.FileName;
            string downloadFilePath = Path.Combine(localPath, fileName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);
            blobClient.DownloadTo(downloadFilePath);

            // Append the new C# class to the local file
            using (StreamWriter sw = System.IO.File.AppendText(downloadFilePath))
            {
                sw.WriteLine();
                sw.Write("public class " + name + " { ");
                sw.Write(fields);
                sw.Write(" } ");
            }

            // Upload file to Blob storage
            blobClient.Upload(downloadFilePath, overwrite: true);
        }
    }

    // Collections of job-specific settings
    public class AzureBlobSettings
    {
        public string ConnectionString { get; set; }
        public string FileName { get; set; }
    }
}