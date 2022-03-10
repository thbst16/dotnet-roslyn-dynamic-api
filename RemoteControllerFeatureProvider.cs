using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using dotnet_roslyn_dynamic_api.Controllers;

namespace dotnet_roslyn_dynamic_api
{
    public class RemoteControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {   
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var remoteCode = new HttpClient().GetStringAsync("https://gist.githubusercontent.com/thbst16/fec3033c371b2a31ce7c852e2ef89bf8/raw/29cb1b0744d238fe65c34bb028cb5b12dfd6ff83/gistfile1.txt").GetAwaiter().GetResult();
            if (remoteCode != null)
            {
                var compilation = CSharpCompilation.Create("DynamicAssembly", new[] { CSharpSyntaxTree.ParseText(remoteCode) },
                    new[] {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(RemoteControllerFeatureProvider).Assembly.Location)
                    },
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    var emitResult = compilation.Emit(ms);

                    if (!emitResult.Success)
                    {
                        // handle, log errors etc
                        Debug.WriteLine("Compilation failed!");
                        return;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(ms.ToArray());
                    var candidates = assembly.GetExportedTypes();

                    foreach (var candidate in candidates)
                    {
                        feature.Controllers.Add(typeof(BaseController<>).MakeGenericType(candidate).GetTypeInfo());
                        GlobalVariables.GeneratedClasses.Add(candidate.ToString());
                    }
                }
            }
        }
    }
}