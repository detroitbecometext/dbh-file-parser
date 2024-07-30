using FileParser.Console.Commands;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using FileParser.Console.Infrastructure;
using FileParser.Core;

var services = new ServiceCollection();
services.AddServices();

var registrar = new TypeRegistrar(services);

var app = new CommandApp<ExtractCommand>(registrar);

app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

return app.Run(args);

