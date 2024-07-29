using FileParser.Core.Extraction;
using FileParser.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace FileParser.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IExecutableFolderPathProvider, AppContextExecutableFolderPathProvider>();

        services.AddSingleton<IExtractionService, ExtractionService>();

        return services;
    }
}
