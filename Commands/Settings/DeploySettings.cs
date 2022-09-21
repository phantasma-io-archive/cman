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
        
        [CommandOption("--nexus <value>")]
        [Description("The nexus name, simnet / testnet / mainnet")]
        [ValidateString]
        public string? Nexus { get; set; }

        [CommandOption("--token")]
        [Description("The smart contraft if it's a token")]
        [ValidateBool]
        public bool Token { get; set; } = false;

        [CommandOption("--update")]
        [Description("The smart contraft if it's a token")]
        [ValidateBool]
        public bool Update { get; set; } = false;
    }
}
