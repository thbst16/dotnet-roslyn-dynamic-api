# dotnet-roslyn-dynamic-api

Generates a dynamic REST-based API using the Roslyn Compiler as a Service (CaaS). API can be generated at startup by referring to an external file with class defintions or at runtime, by passing class defintions into the entity creation API. All APIs persist their data using an in-memory datastore.

# Usage

The image below represents notionally how Roslyn is invoked to dynamically add new assemblies and the resultant APIs:

* **Static Allocation** - Ons startup, only the Entity API is available. The API is served from the Base.dll. The Entity API contains a GET method to list the other APIs provisioned dynamically and a POST API method to create new APIs at runtime.
* **Dynamic Allocation** - At process startup, a call is made to retrieve API definitions (C# classes) from a Gist file. These service(s) are combiled by Roslyn into Dynamic.dll and served up as new APIs with their own GET and POST methods to store and access data.
* **Runtime Allocation** - Calling the Entity POST method allows new APIs to be defined dynamically at runtime. The call accepts an API name and definition (C# classes) as parameters and uses Roslyn to dynamically compile these methods into a new assembly (Runtime.dll) that exposes GET and POST methods for the new API.

![roslyn api allocations](https://s3.amazonaws.com/s3.beckshome.com/20220311-dotnet-roslyn-dynamic-api-allocations.jpg)

# Motivation and Credits

Code generation and metaprogramming with Roslyn, especially in conjuntion with ASP.NET Core, is one of the dark corners of the .NET landscape. Two articles in particular illuminated and informed me in my understanding of Roslyn:
* [Generic and Dynamically Generated Controllers in ASP.NET Core MVC](https://www.strathweb.com/2018/04/generic-and-dynamically-generated-controllers-in-asp-net-core-mvc/)
* [ASP.NET Core: Add Controllers at Runtime and Detecting Changes Done by Others](https://laptrinhx.com/asp-net-core-add-controllers-at-runtime-and-detecting-changes-done-by-others-2489525592/)