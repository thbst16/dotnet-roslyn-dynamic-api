# dotnet-roslyn-dynamic-api
[![Build Status](https://beckshome.visualstudio.com/dotnet-roslyn-dynamic-api/_apis/build/status/thbst16.dotnet-roslyn-dynamic-api?branchName=main)](https://beckshome.visualstudio.com/dotnet-roslyn-dynamic-api/_build/latest?definitionId=11&branchName=main)
![Docker Image Version (latest by date)](https://img.shields.io/docker/v/thbst16/dotnet-roslyn-dynamic-api?logo=docker)

Generates a dynamic REST-based API using the Roslyn Compiler as a Service (CaaS). API can be generated at startup by referring to an external file with class defintions or at runtime, by passing class defintions into the entity creation API. All APIs persist their data using an in-memory datastore.

# Usage

The image below represents notionally how Roslyn is invoked to dynamically add new assemblies and the resultant APIs:

* **Static Allocation** - Ons startup, only the Entity API is available. The API is served from the Base.dll. The Entity API contains a GET method to list the other APIs provisioned dynamically and a POST API method to create new APIs at runtime.
* **Dynamic Allocation** - At process startup, a call is made to retrieve API definitions (C# classes) from a Gist file. These service(s) are combiled by Roslyn into Dynamic.dll and served up as new APIs with their own GET and POST methods to store and access data.
* **Runtime Allocation** - Calling the Entity POST method allows new APIs to be defined dynamically at runtime. The call accepts an API name and definition (C# classes) as parameters and uses Roslyn to dynamically compile these methods into a new assembly (Runtime.dll) that exposes GET and POST methods for the new API.

![roslyn api allocations](https://s3.amazonaws.com/s3.beckshome.com/20220311-dotnet-roslyn-dynamic-api-allocations.jpg)

# Impact and Future

This project has, at it's kernel, all the functionality necessary to deal with rendering and managing a dynamic API with startup and runtime API definition capabilities. The hard work is done. What remains is several areas of refinement:

* **Configurability** - Defining the API through a GIST file and requiring C# class definitions for dynamic and runtime API definition presents a very low level of abstraction for the user. These functions need to be moved to key-value pair defintions that can be passed via the API and persisted in a readily accessible online mechanism, such as a database.
* **Dynamic User Interface** - The next step for dynamic generation capabilities is moving from APIs to user interfaces. Since the generation occurs at the controller level in ASP.NET Core, dynamic generation can be applied to any of the ASP.NET view technologies that use controllers. Ideally suited as a technology here is Server-hosted Blazor. This technology provides a dynamic front-end controlled by a single C# code base so that there's no need to generate seperate front-end Javascript code. Read [here](https://alistapart.com/article/the-future-of-web-software-is-html-over-websockets/) for a sense of what this may look like as html over websockets is exactly the technology that server-hosted Blazor uses.

# Motivation and Credits

Code generation and metaprogramming with Roslyn, especially in conjuntion with ASP.NET Core, is one of the dark corners of the .NET landscape. Two articles in particular illuminated and informed me in my understanding of Roslyn:
* [Generic and Dynamically Generated Controllers in ASP.NET Core MVC](https://www.strathweb.com/2018/04/generic-and-dynamically-generated-controllers-in-asp-net-core-mvc/)
* [ASP.NET Core: Add Controllers at Runtime and Detecting Changes Done by Others](https://laptrinhx.com/asp-net-core-add-controllers-at-runtime-and-detecting-changes-done-by-others-2489525592/)