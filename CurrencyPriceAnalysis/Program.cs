//make sure ConsoleTables Library is installed
using ConsoleTables;
using CurrencyPriceAnalysis;
using System.Formats.Asn1;

var tracker = new PriceProcess();

var timer = new System.Timers.Timer(30000);

timer.Elapsed += async (sender, e) => await tracker.StartTrackingAsync();
timer.AutoReset = true;
timer.Enabled = true;

Console.ReadLine();

tracker.Close();


