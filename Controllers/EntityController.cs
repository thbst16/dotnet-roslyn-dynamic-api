using System.Reflection;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
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
            // Create Controller Using Roslyn - Method Below
            var dynamicAssembly = CreateController(value.Name, value.Fields);
            if (dynamicAssembly != null)
            {
                // Adds a new application part (e.g. controller, view) to the collection
                partManager.ApplicationParts.Add(new AssemblyPart(dynamicAssembly));
                // Triggers the invalidation of the cached collection of action descriptors
                provider.HasChanged = true;
                provider.TokenSource.Cancel();
            }
            else
            {
                throw new Exception("Controller not generated");
            }
        }

        private Assembly CreateController(string name, string fields)
        {
            // Build the dynamic code from a template
            string templateFileName = @"Controllers/TemplateController.tt";
            string templateFileContent = System.IO.File.ReadAllText(templateFileName);
            string code = templateFileContent.Replace("__name__", name).Replace("__fields__", fields);

            // Produce syntax tree by parsing c# code
            var codeString = SourceText.From(code);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);
            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RouteAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ApiControllerAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ControllerBase).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Storage<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location),
            };

            // Added for .NET 5.0
            Assembly.GetEntryAssembly().GetReferencedAssemblies()
                .ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));
                
            // Sets up Roslyn to compile the C# code to a DLL
            string dllName = name + ".dll";
            var codeRun = CSharpCompilation.Create(dllName,
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            // Uses Roslyn to compile and emit the DLL to memory, wher it can be invoked using reflection
            using (var peStream = new MemoryStream())
            {
                EmitResult result = codeRun.Emit(peStream);
                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error
                    );
                    foreach (Diagnostic fail in failures)
                    {
                        System.Console.WriteLine(fail);
                    }
                    return null;
                }
                
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
                // blobClient.Upload(downloadFilePath, overwrite: true);

                return Assembly.Load(peStream.ToArray());
            }
        }
    }

    // Collections of job-specific settings
    public class AzureBlobSettings
    {
        public string ConnectionString { get; set; }
        public string FileName { get; set; }
    }
}