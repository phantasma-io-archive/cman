using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using cman.Commands.Setting;
using Spectre.Console.Cli;
using Spectre.Console;
using System.Net.Http;
using System.Threading.Tasks;
using Phantasma.Business.VM.Utils;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Domain;
using Phantasma.Core.Numerics;
using Phantasma.Core.Types;
using Phantasma.RpcClient;
using Phantasma.RpcClient.Client;

namespace cman.Commands
{
    public class DeployCommand : AsyncCommand<DeploySettings>
    {
        private ILogger Logger { get; }

        private const string ScriptExtension = ".pvm";

        public override async Task<int> ExecuteAsync(CommandContext context, DeploySettings settings)
        {
            // init phantasma rpc
            var phantasmaService = new PhantasmaRpcService(new RpcClient(new Uri(settings.RpcUrl!), httpClientHandler: new HttpClientHandler { }));

            // read WIF -> KxMn2TgXukYaNXx7tEdjh7qB2YaMgeuKy47j4rvKigHhBuZWeP3r for testing purposes 
            string wif = "";
            if (settings.Wif == null)
                wif = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]WIF[/]:").PromptStyle("red").Secret());
            else
                wif = settings.Wif;

            // create a keyPair
            var keyPair = PhantasmaKeys.FromWIF(wif);
            
            Console.WriteLine($"Address:{keyPair.Address}");

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

            // read the contracts script
            var contractScript = File.ReadAllBytes(fileName);

            // read the contracts abi
            var abiBytes = File.ReadAllBytes(abiFile);

            var abi = ContractInterface.FromBytes(abiBytes);

            // build the deploy script
            var sb = new ScriptBuilder();

            //  we need to allow gas, like for any other transaction
            sb.AllowGas(keyPair.Address, Address.Null, 100000, 999999);

            //  TODO: this is just temporary, the test chain doesn't mint enough KCAL to deploy (KCAL needs to be burned to deploy a contract)
            //sb.MintTokens(DomainSettings.FuelTokenSymbol, keyPair.Address, keyPair.Address, UnitConversion.ToBigInteger(1000000, DomainSettings.FuelTokenDecimals));

            
            // Call the deploy interop function, and pass the deployer address, name, scirpt and abi
            if (!settings.Update)
            {
                if ( !settings.Token)
                    sb.CallInterop("Runtime.DeployContract", keyPair.Address, contractName, contractScript, abiBytes);
                else
                    sb.CallInterop("Nexus.CreateToken", keyPair.Address, contractScript, abiBytes);
            }
            else
            {
                sb.CallInterop("Runtime.UpgradeContract", keyPair.Address, contractName, contractScript, abiBytes);
            }

            sb.SpendGas(keyPair.Address);

            var script = sb.EndScript();

            // create the transaction
            var tx = new Transaction(settings.Nexus, "main", script, Timestamp.Now + TimeSpan.FromMinutes(10), "cman");

            // Mine the transaction (a contract deploy tx needs minimal POW, to avoid spam)
            tx.Mine(ProofOfWork.Minimal);

            // sign the tx
            tx.Sign(keyPair);

            var rawTx = tx.ToByteArray( true);
            
            Console.WriteLine($"HasSignatures: {tx.HasSignatures}");


            var encodedRawTx = Base16.Encode(rawTx);
            
            var tx2  = Transaction.Unserialize(Base16.Decode(encodedRawTx));
            
            Console.WriteLine(tx2?.Script);

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
