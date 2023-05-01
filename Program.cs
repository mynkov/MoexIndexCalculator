using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AngleSharp;
using QuickType;
using QuickTypeTicker;

var checkPriviledgedStocks = true;
File.Delete("output.txt");




var bigSmartLabStocks = await GetSmartLabInfos("https://smart-lab.ru/q/index_stocks/IMOEX/order_by_issue_capitalization/desc/");
//PrintCompanies(bigCompanies, "Big companies:");

var bigAggregates = await GetAggregates(bigSmartLabStocks, checkPriviledgedStocks);
var bigAllInfoViews = GetAllInfoViews(bigAggregates);
PrintAllInfoViews(bigAllInfoViews.AllInfos, bigAllInfoViews.Total, "Big companies:");
//return;


var allSmartLabStocks = await GetSmartLabInfos("https://smart-lab.ru/q/index_stocks/MOEXBMI/order_by_issue_capitalization/desc/");
//PrintCompanies(allCompanies, "All companies:");
var allAggregates = await GetAggregates(allSmartLabStocks, checkPriviledgedStocks);
var wideAllInfoViews = GetAllInfoViews(allAggregates);
PrintAllInfoViews(wideAllInfoViews.AllInfos, wideAllInfoViews.Total, "All companies:");

PrintAllInfoViews(wideAllInfoViews.AllInfos.OrderByDescending(x => x.CalculatedInfo.MyDiffRub), wideAllInfoViews.Total, "Need to buy first:");

var exceptCompanies = allSmartLabStocks.Except(bigSmartLabStocks, new SmartLabInfoComparer()).ToList();
PrintSmartLabInfos(exceptCompanies, "Except companies:");

double? prevIndex = 0;
PrintSmartLabInfos(exceptCompanies.Select(x =>
{
    var isHole = x.Index - prevIndex > 1;
    prevIndex = x.Index;
    return new { Company = x, IsHole = isHole };
}).Where(x => x.IsHole).Select(x => x.Company).ToList(), "Increment except companies holes:");

PrintSmartLabInfos(bigSmartLabStocks.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "Big companies percent diff:");
PrintSmartLabInfos(allSmartLabStocks.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "All companies percent diff:");

Console.ReadLine();

static async Task<List<SmartLabInfo>> GetSmartLabInfos(string url)
{
    var document = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var titles = document.QuerySelectorAll("table tr td:nth-child(3)").Select(m => m.TextContent).ToList();
    var tickers = document.QuerySelectorAll("table tr td:nth-child(3) a").Select(m => m.GetAttribute("Href").Replace("/forum/", "")).ToList();
    var percents = document.QuerySelectorAll("table tr td:nth-child(4)").Select(m => m.TextContent).ToList();
    var prices = document.QuerySelectorAll("table tr td:nth-child(7)").Select(m => m.TextContent).ToList();
    var changesMonth = document.QuerySelectorAll("table tr td:nth-child(10)").Select(m => m.TextContent).ToList();
    var changesYear = document.QuerySelectorAll("table tr td:nth-child(11)").Select(m => m.TextContent).ToList();
    var caps = document.QuerySelectorAll("table tr td:nth-child(12)").Select(m => m.TextContent).ToList();

    var list = titles.Select((x, i) => new
    {
        Title = x,
        Cap = !string.IsNullOrWhiteSpace(caps[i]) ? double.Parse(caps[i].Replace(" ", "")) : 0,
        Percent = double.Parse(percents[i].Replace("%", "")) / 100,
        Ticker = tickers[i],
        Price = double.Parse(prices[i]),
        ChangeMonth = changesMonth[i],
        ChangeYear = changesYear[i]
    }).ToList();

    var capSum = list.GroupBy(x => x.Cap).Sum(x => x.Key);

    var companies = list.GroupBy(x => x.Cap).Select((x, i) => new
            SmartLabInfo(
                i + 1,
                string.Join(", ", x.Select(y => x.Count() > 1 ? $"{y.Title} ({y.Percent:P2}, {y.Price}Р)" : y.Title)),
                x.First().Ticker,
                x.Key,
                x.Last().Price,
                x.Sum(s => s.Percent),
                x.Key / capSum,
                x.Key / capSum - x.Sum(s => s.Percent),
                x.Last().ChangeMonth,
                x.Last().ChangeYear
                )).ToList();

    return companies;
}

static async Task<List<AllInfo>> GetAggregates(List<SmartLabInfo> smartLabStocks, bool checkPriviledgedStocks)
{
    // https://www.tinkoff.ru/api/invest-gw/ca-portfolio/api/v1/user/portfolio/pie-chart
    var text = File.ReadAllText("MyStocks.json");
    var myStocks = JsonSerializer.Deserialize<Stocks>(text, QuickType.Converter.Settings);

    var result = new List<AllInfo>();
    var client = new HttpClient();
    const string searchTickerUrl = "https://www.tinkoff.ru/api/trading/stocks/get?ticker";

    foreach (var smartLabInfo in smartLabStocks)
    {
        try
        {
            TickerInfo tinkoffPrefTickerInfo = null;
            if (checkPriviledgedStocks)
            {
                var resultPref = await client.GetStringAsync($"{searchTickerUrl}={smartLabInfo.Ticker}P");
                tinkoffPrefTickerInfo = JsonSerializer.Deserialize<TickerInfo>(resultPref, QuickTypeTicker.Converter.Settings);
                if (tinkoffPrefTickerInfo.Payload.Code == "TickerNotFound")
                {
                    tinkoffPrefTickerInfo = null;
                }
            }

            var ticker = tinkoffPrefTickerInfo?.Payload.Symbol.Ticker ?? smartLabInfo.Ticker;
            var myStock = myStocks.Issuers.FirstOrDefault(x => x.InstrumentInfo != null && x.InstrumentInfo.Any(x => x.Ticker == ticker));
            var tinkoffTickerInfo = tinkoffPrefTickerInfo ?? JsonSerializer.Deserialize<TickerInfo>(await client.GetStringAsync($"{searchTickerUrl}={ticker}"), QuickTypeTicker.Converter.Settings);
            var moexInfo = await GetMoexInfo(tinkoffTickerInfo.Payload.Symbol.Ticker);

            result.Add(new AllInfo(smartLabInfo, myStock?.ValueAbsolute.Value ?? 0.0, tinkoffTickerInfo.Payload, moexInfo));

        }
        catch (Exception exc)
        {
            throw new Exception(smartLabInfo.Ticker, exc);
        }
    }
    return result;
}

static async Task<MoexInfo> GetMoexInfo(string ticker)
{
    const string searchMoexUrl = "https://iss.moex.com/iss/securities/{0}.jsonp?iss.meta=off&iss.json=extended&callback=JSON_CALLBACK&lang=ru&shortname=1";
    var client = new HttpClient();

    var moexResult = await client.GetStringAsync(string.Format(searchMoexUrl, ticker));

    var listing = 0;
    if (moexResult.Contains(@"""Уровень листинга"", ""value"": ""1"""))
    {
        listing = 1;
    }
    else if (moexResult.Contains(@"""Уровень листинга"", ""value"": ""2"""))
    {
        listing = 2;
    }
    else if (moexResult.Contains(@"""Уровень листинга"", ""value"": ""3"""))
    {
        listing = 3;
    }
    else
    {
        listing = -1;
    }
    return new MoexInfo(listing);
}

static AllInfoViews GetAllInfoViews(List<AllInfo> allInfos)
{
    var myTotalCap = allInfos.Sum(x => x.MyStockCap);
    var totalBuyRub = 0.0;
    var totalBuyCount = 0;
    var models = new List<AllInfoView>();

    foreach (var allInfo in allInfos)
    {
        try
        {
            var smartLabInfo = allInfo.SmartLabInfo;
            var tinkoffTickerInfo = allInfo.TinkoffInfoPayload;
            var tinkoffSymbol = tinkoffTickerInfo.Symbol;

            var myStockCap = allInfo.MyStockCap;

            var myStock = new MyStock(
            myStockCap);

            var myPercent = myStockCap / myTotalCap;
            var myDiff = smartLabInfo.NewPercent - myPercent;

            var restCap = myTotalCap - myStockCap;
            var restPercent = 1 - smartLabInfo.NewPercent;
            var afterBuySum = restCap / restPercent;
            var myStockTargerCap = afterBuySum - restCap;
            var myDiffRub = myStockTargerCap - myStockCap;

            if (myDiffRub > 0)
            {
                totalBuyRub += myDiffRub;
                totalBuyCount++;
            }

            var lotSize = tinkoffSymbol.LotSize;
            var price = smartLabInfo.Price;
            var currency = tinkoffTickerInfo.Prices.Buy?.Currency ?? Currency.Rub;

            var tinkoffInfo = new TinkoffInfo(
            tinkoffSymbol.Ticker,
            lotSize,
            currency,
            tinkoffSymbol.Isin,
            tinkoffTickerInfo.ExchangeStatus,
            tinkoffTickerInfo.IsLowLiquid,
            tinkoffTickerInfo.RiskCategory,
            tinkoffTickerInfo.Reliable);

            var buyPrice = tinkoffTickerInfo.Prices.Buy?.Value;
            var withoutBuyPrice = false;

            if (buyPrice.HasValue)
            {
                if (currency == Currency.Rub)
                {
                    price = buyPrice.Value;
                }
            }
            else
            {
                withoutBuyPrice = true;
            }

            var amountToBuy = myDiffRub / price / lotSize;
            var lotPrice = price * lotSize;

            var calculatedInfo = new CalculatedInfo(
            myPercent,
            myDiff,
            amountToBuy,
            myDiffRub,
            lotPrice,
            price,
            withoutBuyPrice);

            models.Add(new AllInfoView(smartLabInfo, myStock, tinkoffInfo, allInfo.MoexInfo, calculatedInfo));
        }
        catch (Exception exc)
        {
            throw new Exception(allInfo.SmartLabInfo.Ticker, exc);
        }
    }

    return new AllInfoViews(models, new TotalInfo(myTotalCap, totalBuyRub, totalBuyCount));
}

static void PrintAllInfoViews(IEnumerable<AllInfoView> allInfoViews, TotalInfo total, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");


    foreach (var allInfoView in allInfoViews)
    {
        try
        {
            var smartLabInfo = allInfoView.SmartLabInfo;
            var calculatedInfo = allInfoView.CalculatedInfo;
            var myStock = allInfoView.MyStock;
            var tinkoffInfo = allInfoView.TinkoffInfo;
            var moexInfo = allInfoView.MoexInfo;

            var amountToBuy = calculatedInfo.AmountToBuy;
            var amountToBuyText = amountToBuy > 0 ?
            amountToBuy >= 1000 ?
             amountToBuy >= 1000000 ? $"{amountToBuy / 1000000:0}M" : $"{amountToBuy / 1000:0}K" :
              $"{amountToBuy:0}" : "--";

            var lotSize = tinkoffInfo.LotSize;
            var lotSizeText = lotSize != 1 ?
            lotSize >= 1000 ?
             lotSize >= 1000000 ? $"{lotSize / 1000000:0}M" : $"{lotSize / 1000:0}K" :
              $"{lotSize:0}" : "    ";

            if (lotSizeText.Length <= 3)
            {
                lotSizeText += "  ";
            }


            var price = calculatedInfo.Price;
            var priceText = lotSize != 1 ? $"{price / 1000:0.000}" : "    ";
            var lotPriceText = $"{calculatedInfo.LotPrice / 1000:0.000}";

            var myDiffRub = calculatedInfo.MyDiffRub;
            var myDiffRubText = myDiffRub / 100 < 1 && myDiffRub / 100 > -1 ? $"{myDiffRub:+0;-0}  " : $"{myDiffRub:+0;-0}" != "-+0" ? $"{myDiffRub:+0;-0}" : "    ";

            var notRusIsinText = tinkoffInfo.Isin.StartsWith("RU") ? "    " : "NotRu";
            var isLowLiquidText = tinkoffInfo.IsLowLiquid == false ? "    " : "BadLiq";
            var reliableText = tinkoffInfo.Reliable == true ? "    " : "BadRel";
            var riskCategoryText = tinkoffInfo.RiskCategory == 0 ? "     " : $"Risk{tinkoffInfo.RiskCategory}";

            var exchangeStatusText = tinkoffInfo.ExchangeStatus == "Open" ? "    " : tinkoffInfo.ExchangeStatus;
            if (calculatedInfo.WithoutBuyPrice)
            {
                exchangeStatusText += "!";
            }
            var currencyText = tinkoffInfo.Currency == Currency.Rub ? "   " : tinkoffInfo.Currency.ToString();

            var listingText = moexInfo.Listing == 1 ? "     " : $"List{moexInfo.Listing}";

            var changeMonth = smartLabInfo.changeMonth;
            var changeMonthText = changeMonth == "0" ? "0   " : changeMonth == string.Empty ? "     " : changeMonth;

            var changeYear = smartLabInfo.changeYear;
            var changeYearText = changeYear == "0" ? "0   " : changeYear == string.Empty ? "     " : changeYear;

            var line = $"{smartLabInfo.Index}\t{smartLabInfo.NewPercent:P2}\t\t{calculatedInfo.MyPercent:P2}\t{calculatedInfo.MyDiff:+0.00%;-0.00%}\t{myStock.MyStockCap / 1000:0}\t\t{smartLabInfo.Percent:P2}\t{smartLabInfo.PercentDiff:+0.00%;-0.00%}\t\t{smartLabInfo.Cap:0.00}\t{changeYearText}\t{changeMonthText}\t{exchangeStatusText}\thttps://www.tinkoff.ru/invest/stocks/{tinkoffInfo.Ticker}\t{amountToBuyText}\t{myDiffRubText}\t{lotPriceText}\t{priceText}\t{lotSizeText}\t{tinkoffInfo.Isin}\t{notRusIsinText}\t{currencyText}\t{isLowLiquidText}\t{listingText}\t{riskCategoryText}\t{reliableText}\t{smartLabInfo.Title}";
            Console.WriteLine(line);
            File.AppendAllText("output.txt", line + "\n");
        }
        catch (Exception exc)
        {
            throw new Exception(allInfoView.SmartLabInfo.Ticker, exc);
        }
    }


    var totalCapMessage = $"\nMy total cap: {total.MyTotalCap / 1000000:0.000}";
    Console.Write(totalCapMessage);
    File.AppendAllText("output.txt", totalCapMessage);

    var totalBuyRubMessage = $"\nMy total buy: {total.TotalBuyRub / 1000:0}K ({total.TotalBuyCount}шт)";
    Console.Write(totalBuyRubMessage);
    File.AppendAllText("output.txt", totalBuyRubMessage);

    Console.WriteLine();
    Console.WriteLine();
    File.AppendAllText("output.txt", "\n\n");
}

static void PrintSmartLabInfos(List<SmartLabInfo> smartLabInfos, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");

    foreach (var smartLabInfo in smartLabInfos)
    {
        PrintSmartLabInfo(smartLabInfo);
    }
    Console.WriteLine();
    Console.WriteLine();
    File.AppendAllText("output.txt", "\n\n");
}

static void PrintSmartLabInfo(SmartLabInfo smartLabInfo)
{
    var line = $"{smartLabInfo.Index}\t{smartLabInfo.NewPercent:P2}\t{smartLabInfo.Percent:P2}\t{smartLabInfo.PercentDiff:+0.00%;-0.00%}\t{smartLabInfo.Cap:0.00}\thttps://www.tinkoff.ru/invest/stocks/{smartLabInfo.Ticker}\t{smartLabInfo.Price / 1000:0.000}\t{smartLabInfo.Title}";
    Console.WriteLine(line);
    File.AppendAllText("output.txt", line + "\n");
}

public record SmartLabInfo(int Index, string Title, string Ticker, double Cap, double Price, double Percent, double NewPercent, double PercentDiff, string changeMonth, string changeYear);

public record AllInfo(SmartLabInfo SmartLabInfo, double MyStockCap, Payload TinkoffInfoPayload, MoexInfo MoexInfo);

public record AllInfoView(SmartLabInfo SmartLabInfo, MyStock MyStock, TinkoffInfo TinkoffInfo, MoexInfo MoexInfo, CalculatedInfo CalculatedInfo);

public record AllInfoViews(List<AllInfoView> AllInfos, TotalInfo Total);

public record MyStock(double MyStockCap);

public record CalculatedInfo(double MyPercent, double MyDiff, double AmountToBuy, double MyDiffRub, double LotPrice, double Price, bool WithoutBuyPrice);

public record TinkoffInfo(string Ticker, long LotSize, Currency Currency, string Isin, string ExchangeStatus, bool IsLowLiquid, long RiskCategory, bool Reliable);

public record TotalInfo(double MyTotalCap, double TotalBuyRub, int TotalBuyCount);

public record MoexInfo(int Listing);

public class SmartLabInfoComparer : IEqualityComparer<SmartLabInfo>
{
    public bool Equals(SmartLabInfo? x, SmartLabInfo? y)
    {
        return x?.Ticker == y?.Ticker;
    }

    public int GetHashCode([DisallowNull] SmartLabInfo obj)
    {
        return obj.Ticker.GetHashCode();
    }
}