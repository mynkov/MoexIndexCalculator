﻿using System.Diagnostics.CodeAnalysis;
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

static async Task<List<SmartLabInfo>> GetSmartLabInfos(string url, bool fromFile = false)
{
    // var cookie = "_fz_fvdt=1608457937; _ym_uid=1608457938504410383; _fz_uniq=723481060741648593; visitor_id=01685d734d5d81667ed2b560ad0a5aa7; _ym_d=1673781191; _count_uid=d1681243947658431qrfzbfl4asgmlgp16r65jkmheb4ngcjo; cf_chl_2=a541ea4fdfa5476; cf_clearance=ZWAv_bIJCfe91SiVW1VG5WyZoAjCMrnzgqnRDq_HyOo-1687099946-0-160; _fz_ssn=1687099951178556055; _gid=GA1.2.876455185.1687099951; _ym_isad=1; PHPSESSID=6b7a899646c08454c7e037c43e681418; _ga_CWV8L1544Z=GS1.1.1687099951.1.1.1687100220.0.0.0; _ga=GA1.1.1569066822.1673781191";
    // var cookies = new AngleSharp.Io.MemoryCookieProvider();
    // cookies.SetCookie(new AngleSharp.Dom.Url(url), cookie);

    // var client = new HttpClient();
    // client.DefaultRequestHeaders.Add("Cookie", cookie);
    // var tr = await new HttpClient().PostAsync(url, null);
    // var tr1 = await tr.Content.ReadAsStringAsync();

    var html = fromFile ? File.ReadAllText(url.Replace("https://smart-lab.ru/q/index_stocks/", string.Empty).Replace("/order_by_issue_capitalization/desc/", string.Empty) + ".html") : null;
    var document = fromFile
     ? await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(req => req.Content(html))
     : await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var titles = document.QuerySelectorAll("table tr td:nth-child(2)").Select(m => m.TextContent).ToList();
    var tickers = document.QuerySelectorAll("table tr td:nth-child(2) a").Select(m => m.GetAttribute("Href").Replace("/forum/", "")).ToList();
    var percents = document.QuerySelectorAll("table tr td:nth-child(3)").Select(m => m.TextContent).ToList();
    var prices = document.QuerySelectorAll("table tr td:nth-child(6)").Select(m => m.TextContent).ToList();
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

    list.Add(new
    {
        Title = "Газпром нефть",
        Cap = await GetCapFromSmartLab("SIBN"),
        Percent = 0.0,
        Ticker = "SIBN",
        Price = 0.0,
        ChangeMonth = "0.0%",
        ChangeYear = "0.0%"
    });

    list.Add(new
    {
        Title = "ПАО Яковлев (Иркут)",
        Cap = await GetCapFromSmartLab("IRKT"),
        Percent = 0.0,
        Ticker = "IRKT",
        Price = 0.0,
        ChangeMonth = "0.0%",
        ChangeYear = "0.0%"
    });

    list.Add(new
    {
        Title = "ОАК",
        Cap = await GetCapFromSmartLab("UNAC"),
        Percent = 0.0,
        Ticker = "UNAC",
        Price = 0.0,
        ChangeMonth = "0.0%",
        ChangeYear = "0.0%"
    });


    list = list.OrderByDescending(x => x.Cap).ToList();

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

    if (companies.Count == 0)
    {
        return await GetSmartLabInfos(url, true);
    }

    return companies;
}

static async Task<double> GetCapFromSmartLab(string ticker)
{
    string url = $"https://smart-lab.ru/q/{ticker}/f/y/";
    var htmlDocument = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var cap = htmlDocument.QuerySelector("tr[field='market_cap'] td:last-child");
    var text = cap.InnerHtml.Replace(" ", string.Empty).Replace("\t", string.Empty);
    var value = double.Parse(text);
    return value;
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
            if (smartLabInfo.Ticker == "ISKJ")
            {
                continue;
            }

            TickerInfo tinkoffPrefTickerInfo = null;
            if (checkPriviledgedStocks && smartLabInfo.Ticker != "BSPB" && smartLabInfo.Ticker != "SELG")
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
            var lastYearDividend = await GetLastYearDividend(smartLabInfo.Ticker, tinkoffPrefTickerInfo?.Payload.Symbol.Ticker);
            var forecastDividend = await GetForecastDividend(ticker);

            result.Add(new AllInfo(smartLabInfo, myStock?.ValueAbsolute.Value ?? 0.0, tinkoffTickerInfo.Payload, moexInfo, lastYearDividend, forecastDividend));

        }
        catch (Exception exc)
        {
            throw new Exception(smartLabInfo.Ticker, exc);
        }
    }
    return result;
}

static async Task<double> GetForecastDividend(string ticker)
{
    string url = $"https://www.dohod.ru/ik/analytics/dividend/";
    var htmlDocument = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url + ticker.ToLower());

    var forecastDividend = htmlDocument.QuerySelector("tr[class='forecast'] td[class='black11']");

    if (double.TryParse(forecastDividend?.InnerHtml, out var result))
    {
        return result;
    }

    return result;
}

static async Task<double> GetLastYearDividend(string ticker, string prefTicker)
{
    string url = $"https://smart-lab.ru/q/{ticker}/dividend/";
    var htmlDocument = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var dividendRows = htmlDocument.QuerySelectorAll("table[class='simple-little-table financials dividends sort-table'] tr");

    double lastYearDividend = 0.0;

    foreach (var dividendRow in dividendRows)
    {
        var tickerFromRow = dividendRow.Children.First().TextContent;
        var searchTicker = prefTicker ?? ticker;
        if (tickerFromRow == searchTicker)
        {
            var dividentPaymentDateText = dividendRow.Children[1].TextContent;
            if (string.IsNullOrWhiteSpace(dividentPaymentDateText))
                continue;

            var dividendPaymentDate = DateTime.ParseExact(dividentPaymentDateText, "dd.MM.yyyy", null);
            var now = DateTime.Now;
            if (dividendPaymentDate > now.AddYears(-1) && dividendPaymentDate <= now)
            {
                var dividendText = dividendRow.Children[4].Children.First().TextContent;
                var dividend = double.Parse(dividendText.Replace(",", "."));
                lastYearDividend += dividend;
            }
        }
    }
    return lastYearDividend;
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
    var totalDividendYield = 0.0;
    var totalForecastDividendPayment = 0.0;
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

            var lastYearDividend = allInfo.LastYearDividend;
            var dividendYield = lastYearDividend / price;
            var dividendWeighted = smartLabInfo.NewPercent * dividendYield;

            totalDividendYield += dividendWeighted;


            var myStocksCount = myStockCap / price;
            var allForecastDividends = allInfo.ForecastDividend * myStocksCount;
            var forecastYield = allInfo.ForecastDividend / price;
            totalForecastDividendPayment += allForecastDividends;
            

            var dividendInfo = new DividendInfo(
                lastYearDividend,
                dividendYield,
                dividendWeighted,
                allInfo.ForecastDividend,
                allForecastDividends,
                forecastYield);

            models.Add(new AllInfoView(smartLabInfo, myStock, tinkoffInfo, allInfo.MoexInfo, calculatedInfo, dividendInfo));
        }
        catch (Exception exc)
        {
            throw new Exception(allInfo.SmartLabInfo.Ticker, exc);
        }
    }

    return new AllInfoViews(models, new TotalInfo(myTotalCap, totalBuyRub, totalBuyCount, totalDividendYield, totalForecastDividendPayment));
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

            var lastYearDividendText = allInfoView.DividendInfo.LastYearDividend >= 10000 ?
                            $"{allInfoView.DividendInfo.LastYearDividend:0}" :
                            allInfoView.DividendInfo.LastYearDividend > 0 ? $"{allInfoView.DividendInfo.LastYearDividend:0.00}" : "    ";

            var dividendYieldText = allInfoView.DividendInfo.DividendYield > 0 ? $"{allInfoView.DividendInfo.DividendYield:P2}" : "     ";
            var dividendWeightedText = allInfoView.DividendInfo.DividendWeighted > 0 ? $"{allInfoView.DividendInfo.DividendWeighted:P2}" : "     ";

            var forecastYieldText = allInfoView.DividendInfo.ForecastYield > 0 ? $"{allInfoView.DividendInfo.ForecastYield:P2}" : "     ";

            var myStockCapText = myStock.MyStockCap >= 1000000 ? $"{myStock.MyStockCap / 1000:0}" : $"{myStock.MyStockCap / 1000:0}\t";

            var line = $"{smartLabInfo.Index}\t{smartLabInfo.NewPercent:P2}\t\t{calculatedInfo.MyPercent:P2}\t{calculatedInfo.MyDiff:+0.00%;-0.00%}\t{myStockCapText}\t{smartLabInfo.Percent:P2}\t{smartLabInfo.PercentDiff:+0.00%;-0.00%}\t\t{smartLabInfo.Cap:0.00}\t{changeYearText}\t{changeMonthText}\t{dividendYieldText}\t{forecastYieldText}\t{exchangeStatusText}\thttps://www.tinkoff.ru/invest/stocks/{tinkoffInfo.Ticker}\t{amountToBuyText}\t{myDiffRubText}\t{lotPriceText}\t{priceText}\t{lotSizeText}\t{tinkoffInfo.Isin}\t{notRusIsinText}\t{currencyText}\t{isLowLiquidText}\t{listingText}\t{riskCategoryText}\t{reliableText}\t{lastYearDividendText}\t{dividendWeightedText}\t\t{allInfoView.DividendInfo.ForecastDividendOnStock / 1000:0.000}\t{allInfoView.DividendInfo.ForecastDividends * 0.87 / 1000:0}\t{smartLabInfo.Title}";
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

    var totalDividendYieldMessage = $"\nTotal dividend yield: {total.TotalDividendYield:P2} ({total.TotalDividendYield * 0.87:P2} after tax)";
    Console.Write(totalDividendYieldMessage);
    File.AppendAllText("output.txt", totalDividendYieldMessage);


    var totalForecastDividendPaymentMessage = $"\nTotal forecast dividend payment: {total.TotalForecastDividendPayment / total.MyTotalCap:P2}, ({total.TotalForecastDividendPayment / total.MyTotalCap * 0.87:P2} after tax), {total.TotalForecastDividendPayment * 0.87:0} per year, {total.TotalForecastDividendPayment * 0.87 / 12:0} per month";
    Console.Write(totalForecastDividendPaymentMessage);
    File.AppendAllText("output.txt", totalForecastDividendPaymentMessage);

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

public record AllInfo(SmartLabInfo SmartLabInfo, double MyStockCap, Payload TinkoffInfoPayload, MoexInfo MoexInfo, double LastYearDividend, double ForecastDividend);

public record AllInfoView(SmartLabInfo SmartLabInfo, MyStock MyStock, TinkoffInfo TinkoffInfo, MoexInfo MoexInfo, CalculatedInfo CalculatedInfo, DividendInfo DividendInfo);

public record AllInfoViews(List<AllInfoView> AllInfos, TotalInfo Total);

public record MyStock(double MyStockCap);

public record CalculatedInfo(double MyPercent, double MyDiff, double AmountToBuy, double MyDiffRub, double LotPrice, double Price, bool WithoutBuyPrice);

public record TinkoffInfo(string Ticker, long LotSize, Currency Currency, string Isin, string ExchangeStatus, bool IsLowLiquid, long RiskCategory, bool Reliable);

public record TotalInfo(double MyTotalCap, double TotalBuyRub, int TotalBuyCount, double TotalDividendYield, double TotalForecastDividendPayment);

public record MoexInfo(int Listing);

public record DividendInfo(double LastYearDividend, double DividendYield, double DividendWeighted, double ForecastDividendOnStock, double ForecastDividends, double ForecastYield);

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