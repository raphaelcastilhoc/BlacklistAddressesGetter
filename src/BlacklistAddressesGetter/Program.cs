using BlacklistAddressesGetter.DTOs;
using BlacklistAddressesGetter.Handlers;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botToken = "6854465663:AAFKgnrwp6CWjgQzy7_-Xc7dqNlteeAabWU";
var botClient = new TelegramBotClient(botToken);

var allowedUpdateTypes = new[] { UpdateType.ChannelPost };
int lastProcessedUpdateId = 0;

var _addressRegex = new Regex(@"\b0x[a-fA-F0-9]{40}\b", RegexOptions.Compiled);

List<KeyValuePair<string, TelegramMessageDto>> _contextMessages = new List<KeyValuePair<string, TelegramMessageDto>>();

while (true)
{
    var updates = await botClient.GetUpdatesAsync(offset: lastProcessedUpdateId, allowedUpdates: allowedUpdateTypes);

    foreach (var update in updates)
    {
        if (update.ChannelPost != null)
        {
            await HandlePossiblyMaliciousAddress(update);
        }
    }

    var lastUpdate = updates.LastOrDefault();
    lastProcessedUpdateId = lastUpdate != null ? lastUpdate.Id + 1 : lastProcessedUpdateId;

    await Task.Delay(1000);
}

async Task HandlePossiblyMaliciousAddress(Update update)
{
    var messageText = update.ChannelPost.Text;
    Console.WriteLine($"Received message '{messageText}'");

    if (string.IsNullOrEmpty(messageText))
    {
        return;
    }

    Match addressMatch = _addressRegex.Match(messageText);
    if (addressMatch.Success)
    {
        var possibleMaliciousAddress = addressMatch.Value;
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"Found address '{possibleMaliciousAddress}'");

        var telegramMessage = new TelegramMessageDto
        {
            ChannelUsername = update.ChannelPost.Chat.Username,
            Message = messageText
        };

        _contextMessages.Add(new KeyValuePair<string, TelegramMessageDto>(possibleMaliciousAddress, telegramMessage));

        var differentChannelsTalkingAboutSameAddressCount = _contextMessages
            .Where(x => x.Key == possibleMaliciousAddress)
            .DistinctBy(x => x.Value.ChannelUsername)
            .Count();

        if (differentChannelsTalkingAboutSameAddressCount >= 3)
        {
            var contextMessages = _contextMessages
            .Where(x => x.Key == possibleMaliciousAddress)
            .Select(x => x.Value.Message)
            .ToArray();

            var addressIsMalicious = await AIHandler.CheckIfAddressIsMaliciousAsync(addressMatch.Value, contextMessages);
            if (addressIsMalicious)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Address '{possibleMaliciousAddress}' is malicious.");

                await SendAddressToBlacklist(possibleMaliciousAddress);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Address '{possibleMaliciousAddress}' is not malicious.");
            }

            _contextMessages.RemoveAll(x => x.Key == possibleMaliciousAddress);
        }
    }
}


async Task SendAddressToBlacklist(string newBlackListAddress)
{
    // Initialize the account with your private key
    var privateKey = "0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
    var account = new Nethereum.Web3.Accounts.Account(privateKey);

    // Create an instance of Web3 with the account, which will use the account to sign transactions
    var web3 = new Web3(account, "http://127.0.0.1:8545");

    // The address of the contract
    var contractAddress = "0x5fbdb2315678afecb367f032d93f642f64180aa3";

    // Prepare the function message
    var addBlackListFunctionMessage = new AddBlackListFunction
    {
        Address = newBlackListAddress // Replace with the address you want to add to the blacklist
    };

    // Create a transaction handler for the AddBlackListFunction
    var transactionHandler = web3.Eth.GetContractTransactionHandler<AddBlackListFunction>();

    // Send the transaction and wait for the receipt
    var transactionReceipt = await transactionHandler.SendRequestAndWaitForReceiptAsync(contractAddress, addBlackListFunctionMessage);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Transaction successful. Transaction Hash: " + transactionReceipt.TransactionHash);
}

[Function("addBlackList")]
public class AddBlackListFunction : FunctionMessage
{
    [Parameter("address", "_address", 1)]
    public string Address { get; set; }
}

