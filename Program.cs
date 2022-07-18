using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using cman.Commands;
using Spectre.Console.Cli;
using Spectre.Cli.Extensions.DependencyInjection;

IServiceCollection? serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
            configure
                .AddSimpleConsole(opts =>
                {
                    opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                }));

using DependencyInjectionRegistrar? registrar = new DependencyInjectionRegistrar(serviceCollection);
CommandApp? app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.ValidateExamples();

        config.AddCommand<DeployCommand>("deploy")
                .WithDescription("Deploy a phantasma contract")
                .WithExample(new[] { "deploy" });
    });

return await app.RunAsync(args);
