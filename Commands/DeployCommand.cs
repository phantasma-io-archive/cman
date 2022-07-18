using Microsoft.Extensions.Logging;
using cman.Commands.Setting;
using Spectre.Console.Cli;
using Spectre.Console;
using Phantasma.Core;
using Phantasma.Business;
using Phantasma.Shared.Types;
using Phantasma.RpcClient;
using Phantasma.RpcClient.Client;
using System.Net;

namespace cman.Commands
{
    public class DeployCommand : AsyncCommand<DeploySettings>
    {
        private ILogger Logger { get; }

        private const string ScriptExtension = ".pvm";

        public override async Task<int> ExecuteAsync(CommandContext context, DeploySettings settings)
        {
            var phantasmaService = new PhantasmaRpcService(new RpcClient(new Uri(settings.RpcUrl!), httpClientHandler: new HttpClientHandler { }));

            var wif = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]WIF[/]:").PromptStyle("red").Secret());

            var keyPair = PhantasmaKeys.FromWIF(wif);
            Console.WriteLine("skdfljlasdkfjklj");

            var fileName = settings.Contract;

            if (!File.Exists(fileName))
            {
                AnsiConsole.WriteException(new Exception("Provided file does not exist"), ExceptionFormats.ShortenEverything);
                return await Task.FromResult(-1);
            }

            if (!fileName.EndsWith(ScriptExtension))
            {
                AnsiConsole.WriteException(new Exception($"Provided file is not a compiled {ScriptExtension} script"), ExceptionFormats.ShortenEverything);
                return await Task.FromResult(-1);
            }

            var abiFile = fileName.Replace(ScriptExtension, ".abi");

            if (!File.Exists(abiFile))
            {
                AnsiConsole.WriteException(new Exception($"No ABI file {abiFile} that matches provided script file"), ExceptionFormats.ShortenEverything);
                return await Task.FromResult(-1);
            }

            var contractName = Path.GetFileNameWithoutExtension(fileName);

            var contractScript = File.ReadAllBytes(fileName);
            var abiBytes = File.ReadAllBytes(abiFile);

            var abi = ContractInterface.FromBytes(abiBytes);

            var sb = new ScriptBuilder();

            sb.AllowGas(keyPair.Address, Address.Null, 100000, 9999);
            sb.MintTokens(DomainSettings.FuelTokenSymbol, keyPair.Address, keyPair.Address, UnitConversion.ToBigInteger(1000000, DomainSettings.FuelTokenDecimals));
            sb.CallInterop("Runtime.DeployContract", keyPair.Address, contractName, contractScript, abiBytes);
            sb.SpendGas(keyPair.Address);
            var script = sb.EndScript();

            var tx = new Transaction("mainnet", "main", script, Timestamp.Now + TimeSpan.FromMinutes(5), "cman");
            tx.Mine(ProofOfWork.Minimal);
            tx.Sign(keyPair);

            var rawTx = tx.ToByteArray(true);
            var encodedRawTx = Base16.Encode(rawTx);

            var txHash = await phantasmaService.SendRawTx.SendRequestAsync(encodedRawTx, "1");
            AnsiConsole.WriteLine($"Transaction hash: {txHash}");

            return await Task.FromResult(0);
        }

        public DeployCommand(ILogger<DeployCommand> logger)
        {
            Logger = logger;
        }
    }
}
