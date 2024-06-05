using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(wordle_server.Startup))]

namespace wordle_server
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<IBlobDAO, BlobDAO>();
            builder.Services.AddSingleton<ITableDAO, TableDAO>();
            builder.Services.AddScoped<IStorageService, StorageService>();
            builder.Services.AddScoped<IGameLogicService, GameLogicService>();
            builder.Services.AddScoped<IIdentifierService, IdentifierService>();
        }
    }
}
