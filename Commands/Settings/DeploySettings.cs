using System.ComponentModel;
using cman.Commands.Validation;
using Spectre.Console.Cli;

namespace cman.Commands.Setting
{
    public class DeploySettings : CommandSettings
    {
        [CommandOption("--contract <value>")]
        [Description("Contract pvm file to deploy")]
        [ValidateString]
        public string? Contract { get; set; }

        [CommandOption("--rpc-url <value>")]
        [Description("The node used to deploy")]
        [ValidateString]
        public string? RpcUrl { get; set; }
    }
}
