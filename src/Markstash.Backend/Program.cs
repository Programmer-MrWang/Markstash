using Markstash.Backend;

var builder = BackendApplication.CreateBuilder(args);
var app = BackendApplication.Build(builder);
await app.RunAsync();

public partial class Program;
