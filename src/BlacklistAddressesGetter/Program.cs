using BlacklistAddressesGetter.DTOs;
using BlacklistAddressesGetter.Handlers;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
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

    Match addressMatch = _addressRegex.Match(messageText);
    if (addressMatch.Success)
    {
        var possibleMaliciousAddress = addressMatch.Value;
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
                Console.WriteLine($"Address '{possibleMaliciousAddress}' is malicious.");

                await SendAddressToBlacklist(possibleMaliciousAddress);
            }
            else
            {
                Console.WriteLine($"Address '{possibleMaliciousAddress}' is not malicious.");
            }

            _contextMessages.RemoveAll(x => x.Key == possibleMaliciousAddress);
        }
    }
}

async Task SendAddressToBlacklist(string address)
{
    var web3 = new Web3("http://127.0.0.1:8545");

    var contractAddress = "0xYourContractAddress";
    var contractAbi = "YourContractAbiJson";

    var contract = web3.Eth.GetContract(contractAbi, contractAddress);

    var function = contract.GetFunction("yourFunctionName");
    var result = await function.CallAsync<string>(address);

    var privateKey = "yourPrivateKey";

    // Create a new transaction
    var transactionInput = new TransactionInput
    {
        From = "yourSenderAddress",
        To = "recipientAddress",
        Gas = new HexBigInteger(21000),
        GasPrice = new HexBigInteger(20000000000),
        Value = new HexBigInteger(Web3.Convert.ToWei(1, UnitConversion.EthUnit.Ether)),
    };

    // Sign the transaction locally
    var signedTransaction = web3.Eth.Transactions.SendTransaction.SendRequestAsync(transactionInput, privateKey).Result;

    // Send the signed transaction to the network
    var transactionHash = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedTransaction);
}