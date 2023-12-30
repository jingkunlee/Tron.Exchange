using System.Diagnostics;
using System.Text;

namespace Tron.Exchange;

using Newtonsoft.Json;
using Tron.Wallet;
using Tron.Wallet.Accounts;
using Tron.Wallet.Crypto;

internal static class Program {
    private static async Task Main() {
        #region Initial

        Config config;

        try {
            Console.WriteLine();

            var processes = Process.GetProcesses();
            var currentProcess = Process.GetCurrentProcess();
            if (processes.Any(process => process.ProcessName == currentProcess.ProcessName && process.Id != currentProcess.Id)) return;


            var configPath = Path.GetFullPath("./config.json");
            if (!File.Exists(configPath)) throw new Exception(" 配置文件 config.json 不存在..");

            using (var streamReader = new StreamReader(configPath)) {
                config = JsonConvert.DeserializeObject<Config>(await streamReader.ReadToEndAsync()) ?? new Config();
            }

            if (config == null) throw new Exception(" 配置文件 config.json 读取失败..");
            if (string.IsNullOrEmpty(config.PrivateKey)) throw new Exception(" 未配置 PrivateKey 值..");

            Console.WriteLine($" 请核对监控地址 {config.Address} ，请按回车键启动程序..");
            var keyInfo = Console.ReadKey();
            if (keyInfo.Key != ConsoleKey.Enter) return;
        } catch (Exception exception) {
            Console.WriteLine(exception.Message);
            Console.WriteLine(" 按任意键退出..");
            Console.ReadKey();
            return;
        }

        #endregion

        var blockNumber = 0;

        while (true) {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try {
                string responseString;
                if (blockNumber == 0) {
                    const string url = "https://api.trongrid.io/wallet/getnowblock";
                    responseString = HttpClientHelper.Get(url);
                } else {
                    const string url = "https://api.trongrid.io/wallet/getblockbynum";
                    var requestBody = new { num = blockNumber + 1 };
                    responseString = HttpClientHelper.Post(url, JsonConvert.SerializeObject(requestBody), Encoding.UTF8);
                }

                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObject == null) throw new ThreadSleepException();
                if (responseObject.blockID == null) throw new ThreadSleepException();
                if (responseObject.block_header == null) throw new ThreadSleepException();

                blockNumber = (int)responseObject.block_header.raw_data.number;
                var blockHash = (string)responseObject.blockID;
                var millisecondTimestamp = (long)responseObject.block_header.raw_data.timestamp;
                Console.WriteLine($" {blockNumber}\t{blockHash}\t{DateTimeExtensions.GetDatetimeFromMillisecondTimestamp(millisecondTimestamp):yyyy-MM-dd HH:mm:ss}");

                if (responseObject.transactions == null || responseObject.transactions.Count == 0) continue;
                foreach (var transaction in responseObject.transactions) {
                    var ret = transaction.ret;
                    if (ret == null) continue;
                    if (ret.Count == 0) continue;
                    if (ret[0].contractRet == null || ret[0].contractRet != "SUCCESS") continue;

                    var rawData = transaction.raw_data;
                    if (rawData == null) continue;

                    var contracts = rawData.contract;
                    if (contracts == null) continue;
                    if (contracts.Count == 0) continue;

                    var contract = contracts[0];
                    if (contract == null) continue;

                    var parameter = contract.parameter;
                    if (parameter == null) continue;

                    var value = parameter.value;
                    if (value == null) continue;

                    var type = (string)contract.type;
                    switch (type) {
                        case "TriggerSmartContract": {
                                if (value.contract_address != null && (string)value.contract_address == "41a614f803b6fd780986a42c78ec9c7f77e6ded13c") {
                                    var data = (string)value.data;
                                    switch (data[..8]) {
                                        case "a9059cbb": {
                                                var fromAddress = Base58Encoder.EncodeFromHex((string)value.owner_address, 0x41);
                                                var toAddress = Base58Encoder.EncodeFromHex(((string)value.data).Substring(8, 64), 0x41);
                                                var amount = Convert.ToInt64(((string)value.data).Substring(72, 64), 16);
                                                var transferAmount = amount / new decimal(1000000);

                                                if (config == null || config.Address != toAddress) break;

                                                Console.WriteLine($" Transfer in {fromAddress}\t{toAddress}\t{transferAmount} USDT");

                                                var tradeAmount = transferAmount * config.ExchangeRate;

                                                var tuple = GetAccountByOnline(config.Address);
                                                if (tuple.Item1 < tradeAmount) {
                                                    Console.WriteLine($"地址 TRX 余额 {tuple.Item1} 不足..");
                                                    break;
                                                }

                                                var result = await TrxTransferAsync(config.PrivateKey, fromAddress, (long)(tradeAmount * 1000000L));
                                                Console.WriteLine($" {JsonConvert.SerializeObject(result)}");
                                                break;
                                            }
                                    }
                                }
                                break;
                            }
                        default: {
                                continue;
                            }
                    }
                }
            } catch (ThreadSleepException) {
                if (stopWatch.ElapsedMilliseconds >= 1000) continue;
                Thread.Sleep((int)(1000 - stopWatch.ElapsedMilliseconds));
            } catch (Exception exception) {
                Console.WriteLine($" {exception.Message}");
                if (stopWatch.ElapsedMilliseconds >= 1000) continue;
                Thread.Sleep((int)(1000 - stopWatch.ElapsedMilliseconds));
            }

            if (stopWatch.ElapsedMilliseconds >= 2500) continue;
            Thread.Sleep((int)(2500 - stopWatch.ElapsedMilliseconds));
        }
    }

    #region GetAccountByOnline

    private static Tuple<decimal, decimal> GetAccountByOnline(string address) {
        var trxBalance = new decimal(0);
        var etherBalance = new decimal(0);

        var result = new Tuple<decimal, decimal>(trxBalance, etherBalance);

        var responseString = HttpClientHelper.Get($"https://api.trongrid.io/v1/accounts/{address}");
        if (string.IsNullOrEmpty(responseString)) return result;

        var responseObject = JsonConvert.DeserializeObject<dynamic>(responseString);
        if (responseObject == null) return result;

        if ((bool)responseObject.success != true) return result;
        if (responseObject.data == null || responseObject.data.Count == 0) return result;

        var account = responseObject.data[0];

        var balance = account.balance;
        if (balance != null) trxBalance = Convert.ToInt64(balance) / new decimal(1000000);

        var tokens = account.trc20;
        if (tokens != null) {
            foreach (var token in tokens) {
                var etherValue = token.TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t;
                if (etherValue != null) {
                    etherBalance = Convert.ToInt64(etherValue) / new decimal(1000000);
                }
            }
        }

        return new Tuple<decimal, decimal>(trxBalance, etherBalance);
    }

    #endregion

    #region TrxTransferAsync

    private static async Task<dynamic> TrxTransferAsync(string privateKey, string to, long amount) {
        var record = TronServiceExtension.GetRecord();
        var transactionClient = record.TronClient?.GetTransaction();

        var account = new TronAccount(privateKey, TronNetwork.MainNet);

        var transactionExtension = await transactionClient?.CreateTransactionAsync(account.Address, to, amount)!;

        var transactionId = transactionExtension.Txid.ToStringUtf8();

        var transactionSigned = transactionClient.GetTransactionSign(transactionExtension.Transaction, privateKey);
        var returnObj = await transactionClient.BroadcastTransactionAsync(transactionSigned);

        return new { Result = returnObj.Result, Message = returnObj.Message, TransactionId = transactionId };
    }

    #endregion
}