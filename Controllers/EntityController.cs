using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using dotnet_roslyn_dynamic_api;
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
            var dynamicAssembly = CreateController(value.Name);
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

        private Assembly CreateController(string name)
        {
            // Use a simple StringBuilder to create the dynamic code
            string code = new StringBuilder()
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using Microsoft.AspNetCore.Mvc;")
                .AppendLine("using dotnet_roslyn_dynamic_api.Models;")
                .AppendLine("namespace dotnet_roslyn_dynamic_api")
                .AppendLine("{")
                .AppendLine("[Route(\"api/[controller]\")]")
                .AppendLine(string.Format("public class {0}Controller : ControllerBase", name))
                .AppendLine("{")
                .AppendLine(string.Format("private Storage<{0}> _storage;", name))
                .AppendLine("{")
                .AppendLine("_storage = storage;")
                .AppendLine("}")
                .AppendLine("[HttpGet]")
                .AppendLine(string.Format("public IEnumerable<{0}> Get()", name))
                .AppendLine("{")
                .AppendLine("return _storage.GetAll();")
                .AppendLine("}")
                .AppendLine("[HttpGet(\"{id}\")]")
                .AppendLine(string.Format("public {0} Get(Guid id)", name))
                .AppendLine("{")
                .AppendLine("return _storage.GetById(id);")
                .AppendLine("}")
                .AppendLine("[HttpPost(\"{id}\")]")
                .AppendLine(String.Format("public void Post(Guid id, [FromBody]{0} value)", name))
                .AppendLine("{")
                .AppendLine("_storage.Add(id, value);")
                .AppendLine("}")
                .AppendLine("}")
                .AppendLine("")
                .AppendLine(String.Format("public class {0}", name))
                .AppendLine("{")
                .AppendLine("public Guid id {get;set;}")
                .AppendLine("public string Name {get;set;}")
                .AppendLine("}")
                .AppendLine("}")
                .ToString();

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
                return Assembly.Load(peStream.ToArray());
            }
        }
    }
}