using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleTables;
using Spectre.Console;

namespace CurrencyPriceAnalysis
{
    public class PriceProcess
    {
        public List<PriceInfo> SellPrices { get; set; }
        public List<PriceInfo> BuyPrices { get; set; }

        private FileStream csvFileStream;
        private StreamWriter csvWriter;

        private FileStream resultFileStream;
        private StreamWriter resultWriter;

        public PriceProcess()
        {
            SellPrices = new List<PriceInfo>();
            BuyPrices = new List<PriceInfo>();

            csvFileStream = new FileStream("prices.csv", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            csvWriter = new StreamWriter(csvFileStream);
            csvWriter.AutoFlush = true;

            resultFileStream = new FileStream("predictions.csv", FileMode.Create, FileAccess.Write, FileShare.Write);
            resultWriter = new StreamWriter(resultFileStream);
            resultWriter.AutoFlush = true;

            if (csvFileStream.Length == 0)
            {
                csvWriter.WriteLine("Timestamp,SellPrice,BuyPrice");
            }

            resultWriter.WriteLine("Timestamp|SellPrice|BuyPrice|PredictedPrice|Difference");
        }

        public async Task StartTrackingAsync()
        {
            var url = "https://api.kucoin.com/api/v1/market/stats?symbol=BTC-USDT";
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd|BTC-USD|HH:mm:ss");


            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var obj = JsonSerializer.Deserialize<MarketStats>(content, options);

            decimal sellPrice;
            decimal buyPrice;

            if (decimal.TryParse(obj?.Data?.Sell, out sellPrice))
            {
                SellPrices.Add(new PriceInfo { Timestamp = DateTime.Now, Price = sellPrice });
            }

            if (decimal.TryParse(obj?.Data?.Buy, out buyPrice))
            {
                BuyPrices.Add(new PriceInfo { Timestamp = DateTime.Now, Price = buyPrice });
            }

            var averagePriceOneMinuteAgo = CalculateAveragePrice(1);
            var currentAveragePrice = CalculateAveragePrice(0);
            var predictedPrice = currentAveragePrice + (currentAveragePrice - averagePriceOneMinuteAgo);
            var difference = buyPrice + predictedPrice;
            
            Console.WriteLine("The table below will be updated every 1 minute");
            Console.WriteLine($" '============={timestamp}=============' ");
            var table = new ConsoleTable("Sell Price", "Buy Price", "Predicted price", "Difference", "High Price", "Low Price");
            table.AddRow(sellPrice, buyPrice, predictedPrice, difference, obj.Data.high, obj.Data.low);
            Console.WriteLine(table);

            csvWriter.WriteLine($"{timestamp},{sellPrice},{buyPrice}");

            resultWriter.WriteLine($"{timestamp}|{sellPrice}|{buyPrice}|{predictedPrice}|{difference}");
           // Console.WriteLine($"Difference % : {(buyPrice + predictedPrice) / (predictedPrice) * 100}");

        }

        public decimal CalculateAveragePrice(int minutes)
        {
            var sellPrices = SellPrices.FindAll(price => DateTime.Now.Subtract(TimeSpan.FromMinutes(minutes)) <= price.Timestamp);
            var buyPrices = BuyPrices.FindAll(price => DateTime.Now.Subtract(TimeSpan.FromMinutes(minutes)) <= price.Timestamp);

            if (sellPrices.Count == 0 || buyPrices.Count == 0)
            {
                return 0;
            }

            var averageSellPrice = sellPrices.Average(price => price.Price);
            var averageBuyPrice = buyPrices.Average(price => price.Price);

            return (averageSellPrice + averageBuyPrice) / 2;
        }

        public void Close()
        {
            csvFileStream.Flush();
            csvFileStream.Close();

            resultFileStream.Flush();
            resultFileStream.Close();
        }

    }
}

