using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AngleSharp;
using QuickType;
using QuickTypeTicker;

var checkPriviledgedStocks = true;
File.Delete("output.txt");




var bigCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/IMOEX/order_by_issue_capitalization/desc/");
//PrintCompanies(bigCompanies, "Big companies:");

// https://www.tinkoff.ru/api/invest-gw/ca-portfolio/api/v1/user/portfolio/pie-chart
var text = File.ReadAllText("Stocks.json");
var stocks = JsonSerializer.Deserialize<Stocks>(text, QuickType.Converter.Settings);

var agg = new List<Aggregate>();
var client = new HttpClient();
const string searchTickerUrl = "https://www.tinkoff.ru/api/trading/stocks/get?ticker";
foreach (var company in bigCompanies)
{
    TickerInfo pTickerInfo = null;
    if (checkPriviledgedStocks)
    {
        var resultP = await client.GetStringAsync($"{searchTickerUrl}={company.Ticker}P");
        pTickerInfo = JsonSerializer.Deserialize<TickerInfo>(resultP, QuickTypeTicker.Converter.Settings);
        if (pTickerInfo.Payload.Code == "TickerNotFound")
        {
            pTickerInfo = null;
        }
    }

    var ticker = pTickerInfo?.Payload.Symbol.Ticker ?? company.Ticker;
    var myStock = stocks.Issuers.FirstOrDefault(x => x.InstrumentInfo != null && x.InstrumentInfo.Any(x => x.Ticker == ticker));
    var tickerInfo = pTickerInfo ?? JsonSerializer.Deserialize<TickerInfo>(await client.GetStringAsync($"{searchTickerUrl}={ticker}"), QuickTypeTicker.Converter.Settings);

    agg.Add(new Aggregate(company, myStock ?? new CurrencyElement { ValueAbsolute = new TotalAmountCurrency() }, tickerInfo.Payload));
}

PrintAggregates(agg, "Big companies:");
return;


var allCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/MOEXBMI/order_by_issue_capitalization/desc/");
PrintCompanies(allCompanies, "All companies:");

var exceptCompanies = allCompanies.Except(bigCompanies, new CompanyComparer()).ToList();
PrintCompanies(exceptCompanies, "Except companies:");

double? prevIndex = 0;
PrintCompanies(exceptCompanies.Select(x =>
{
    var isHole = x.Index - prevIndex > 1;
    prevIndex = x.Index;
    return new { Company = x, IsHole = isHole };
}).Where(x => x.IsHole).Select(x => x.Company).ToList(), "Increment except companies holes:");

PrintCompanies(bigCompanies.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "Big companies percent diff:");
PrintCompanies(allCompanies.Where(x => Math.Abs(x.PercentDiff) >= 0.005).OrderByDescending(x => x.PercentDiff).ToList(), "All companies percent diff:");

Console.ReadLine();

static async Task<List<Company>> GetCompanies(string url)
{
    var document = await BrowsingContext.New(Configuration.Default.WithDefaultLoader()).OpenAsync(url);

    var titles = document.QuerySelectorAll("table tr td:nth-child(3)").Select(m => m.TextContent).ToList();
    var tickers = document.QuerySelectorAll("table tr td:nth-child(3) a").Select(m => m.GetAttribute("Href").Replace("/forum/", "")).ToList();
    var percents = document.QuerySelectorAll("table tr td:nth-child(4)").Select(m => m.TextContent).ToList();
    var prices = document.QuerySelectorAll("table tr td:nth-child(7)").Select(m => m.TextContent).ToList();
    var caps = document.QuerySelectorAll("table tr td:nth-child(12)").Select(m => m.TextContent).ToList();

    var list = titles.Select((x, i) => new
    {
        Title = x,
        Cap = double.Parse(caps[i].Replace(" ", "")),
        Percent = double.Parse(percents[i].Replace("%", "")) / 100,
        Ticker = tickers[i],
        Price = double.Parse(prices[i])
    }).ToList();

    var capSum = list.GroupBy(x => x.Cap).Sum(x => x.Key);

    var companies = list.GroupBy(x => x.Cap).Select((x, i) => new
            Company(
                i + 1,
                string.Join(", ", x.Select(y => x.Count() > 1 ? $"{y.Title} ({y.Percent:P2}, {y.Price}Р)" : y.Title)),
                x.First().Ticker,
                x.Key,
                x.Last().Price,
                x.Sum(s => s.Percent),
                x.Key / capSum,
                x.Key / capSum - x.Sum(s => s.Percent)
                )).ToList();

    return companies;
}

static void PrintAggregates(List<Aggregate> aggs, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");

    var myTotalCap = aggs.Sum(x => x.myStock.ValueAbsolute.Value);
    var totalBuyRub = 0.0;
    var totalBuyCount = 0;

    foreach (var agg in aggs)
    {
        var company = agg.company;
        var symbol = agg.tickerInfo.Symbol;
        var myStock = agg.myStock;
        var myStockCap = myStock.ValueAbsolute.Value;

        var myPercent = myStockCap / myTotalCap;
        var myDiff = company.NewPercent - myPercent;

        //var myDiffRubOld = sum * myDiff;

        var restCap = myTotalCap - myStockCap;
        var restPercent = 1 - company.NewPercent;
        var afterBuySum = restCap / restPercent;
        var myStockTargerCap = afterBuySum - restCap;
        var myDiffRub = myStockTargerCap - myStockCap;

        var lotSize = symbol.LotSize;
        var price = agg.tickerInfo.Prices.Buy.Value;
        var amountToBuy = myDiffRub / price / lotSize;
        var amountToBuyText = amountToBuy > 0 ?
        amountToBuy >= 1000 ?
         amountToBuy >= 1000000 ? $"{amountToBuy / 1000000:0}M" : $"{amountToBuy / 1000:0}K" :
          $"{amountToBuy:0}" : "--";


        var lotSizeText = lotSize != 1 ?
        lotSize >= 1000 ?
         lotSize >= 1000000 ? $"{lotSize / 1000000:0}M" : $"{lotSize / 1000:0}K" :
          $"{lotSize:0}" : string.Empty;

        var priceText = lotSize != 1 ? $"{price / 1000:0.000}" : "    ";
        var lotPriceText = $"{price * lotSize / 1000:0.000}";
        var myDiffRubText = $"{myDiffRub:+0;-0}" != "-+0" ? $"{myDiffRub:+0;-0}" : "    ";

        if (myDiffRub > 0)
        {
            totalBuyRub += myDiffRub;
            totalBuyCount++;
        }

        var line = $"{company.Index}\t{company.NewPercent:P2}\t\t{myPercent:P2}\t{myDiff:+0.00%;-0.00%}\t{myStockCap / 1000:0}\t\t{company.Percent:P2}\t{company.PercentDiff:+0.00%;-0.00%}\t\t{company.Cap:0.00}\thttps://www.tinkoff.ru/invest/stocks/{symbol.Ticker}\t{amountToBuyText}\t{myDiffRubText}\t{lotPriceText}\t{priceText}\t{lotSizeText}\t{company.Title}";
        Console.WriteLine(line);
        File.AppendAllText("output.txt", line + "\n");
    }


    var totalCapMessage = $"\nMy total cap: {myTotalCap / 1000000:0.000}";
    Console.Write(totalCapMessage);
    File.AppendAllText("output.txt", totalCapMessage);

    var totalBuyRubMessage = $"\nMy total buy: {totalBuyRub / 1000:0}K ({totalBuyCount}шт)";
    Console.Write(totalBuyRubMessage);
    File.AppendAllText("output.txt", totalBuyRubMessage);

    Console.WriteLine();
    Console.WriteLine();
    File.AppendAllText("output.txt", "\n\n");
}

static void PrintCompanies(List<Company> companies, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");

    foreach (var company in companies)
    {
        PrintCompany(company);
    }
    Console.WriteLine();
    Console.WriteLine();
    File.AppendAllText("output.txt", "\n\n");
}

static void PrintCompany(Company company)
{
    var line = $"{company.Index}\t{company.NewPercent:P2}\t{company.Percent:P2}\t{company.PercentDiff:+0.00%;-0.00%}\t{company.Cap:0.00}\thttps://www.tinkoff.ru/invest/stocks/{company.Ticker}\t{company.Price / 1000:0.000}\t{company.Title}";
    Console.WriteLine(line);
    File.AppendAllText("output.txt", line + "\n");
}

public record Company(int Index, string Title, string Ticker, double Cap, double Price, double Percent, double NewPercent, double PercentDiff);

public record Aggregate(Company company, CurrencyElement myStock, Payload tickerInfo);

public class CompanyComparer : IEqualityComparer<Company>
{
    public bool Equals(Company? x, Company? y)
    {
        return x?.Ticker == y?.Ticker;
    }

    public int GetHashCode([DisallowNull] Company obj)
    {
        return obj.Ticker.GetHashCode();
    }
}