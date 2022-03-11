# dotnet-roslyn-dynamic-api

Generates a dynamic REST-based API using the Roslyn Compiler as a Service (CaaS). API can be generated at startup by referring to an external file with class defintions or at runtime, by passing class defintions into the entity creation API. All APIs persist their data using an in-memory datastore. 

# Motivation and Credits

Code generation and metaprogramming with Roslyn, especially in conjuntion with ASP.NET Core, is one of the dark corners of the .NET landscape. Two articles in particular illuminated and informed me in my understanding of Roslyn:
* [Generic and Dynamically Generated Controllers in ASP.NET Core MVC](https://www.strathweb.com/2018/04/generic-and-dynamically-generated-controllers-in-asp-net-core-mvc/)
* [ASP.NET Core: Add Controllers at Runtime and Detecting Changes Done by Others](https://laptrinhx.com/asp-net-core-add-controllers-at-runtime-and-detecting-changes-done-by-others-2489525592/)