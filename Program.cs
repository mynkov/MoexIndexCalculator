using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AngleSharp;
using QuickType;
using QuickTypeTicker;

var checkPriviledgedStocks = true;
File.Delete("output.txt");




var bigCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/IMOEX/order_by_issue_capitalization/desc/");
//PrintCompanies(bigCompanies, "Big companies:");

var bigAggregates = await GetAggregates(bigCompanies, checkPriviledgedStocks);
var bigViewModels = GetViewModels(bigAggregates);
PrintViewModels(bigViewModels.models, bigViewModels.total, "Big companies:");
//return;


var allCompanies = await GetCompanies("https://smart-lab.ru/q/index_stocks/MOEXBMI/order_by_issue_capitalization/desc/");
//PrintCompanies(allCompanies, "All companies:");
var allAggregates = await GetAggregates(allCompanies, checkPriviledgedStocks);
var allViewModels = GetViewModels(allAggregates);
PrintViewModels(allViewModels.models, allViewModels.total, "All companies:");

PrintViewModels(allViewModels.models.OrderByDescending(x => x.myStock.myDiffRub), allViewModels.total, "Need to buy first:");

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
    var changesMonth = document.QuerySelectorAll("table tr td:nth-child(10)").Select(m => m.TextContent).ToList();
    var changesYear = document.QuerySelectorAll("table tr td:nth-child(11)").Select(m => m.TextContent).ToList();
    var caps = document.QuerySelectorAll("table tr td:nth-child(12)").Select(m => m.TextContent).ToList();

    var list = titles.Select((x, i) => new
    {
        Title = x,
        Cap = double.Parse(caps[i].Replace(" ", "")),
        Percent = double.Parse(percents[i].Replace("%", "")) / 100,
        Ticker = tickers[i],
        Price = double.Parse(prices[i]),
        ChangeMonth = changesMonth[i],
        ChangeYear = changesYear[i]
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
                x.Key / capSum - x.Sum(s => s.Percent),
                x.Last().ChangeMonth,
                x.Last().ChangeYear
                )).ToList();

    return companies;
}

static async Task<List<Aggregate>> GetAggregates(List<Company> companies, bool checkPriviledgedStocks)
{
    // https://www.tinkoff.ru/api/invest-gw/ca-portfolio/api/v1/user/portfolio/pie-chart
    var text = File.ReadAllText("MyStocks.json");
    var stocks = JsonSerializer.Deserialize<Stocks>(text, QuickType.Converter.Settings);

    var result = new List<Aggregate>();
    var client = new HttpClient();
    const string searchTickerUrl = "https://www.tinkoff.ru/api/trading/stocks/get?ticker";

    foreach (var company in companies)
    {
        try
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

            var moexInfo = await GetMoexInfo(tickerInfo.Payload.Symbol.Ticker);

            result.Add(new Aggregate(company, myStock ?? new CurrencyElement { ValueAbsolute = new TotalAmountCurrency() }, tickerInfo.Payload, moexInfo));

        }
        catch (Exception exc)
        {
            throw new Exception(company.Ticker, exc);
        }
    }
    return result;
}

static async Task<Moex> GetMoexInfo(string ticker)
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
    return new Moex(listing);
}

static ViewModels GetViewModels(List<Aggregate> aggs)
{
    var myTotalCap = aggs.Sum(x => x.myStock.ValueAbsolute.Value);
    var totalBuyRub = 0.0;
    var totalBuyCount = 0;
    var models = new List<ViewModel>();

    foreach (var agg in aggs)
    {
        try
        {
            var company = agg.company;
            var tickerInfo = agg.tickerInfo;
            var symbol = tickerInfo.Symbol;

            var myStock = agg.myStock;
            var myStockCap = myStock.ValueAbsolute.Value;

            var myPercent = myStockCap / myTotalCap;
            var myDiff = company.NewPercent - myPercent;

            var restCap = myTotalCap - myStockCap;
            var restPercent = 1 - company.NewPercent;
            var afterBuySum = restCap / restPercent;
            var myStockTargerCap = afterBuySum - restCap;
            var myDiffRub = myStockTargerCap - myStockCap;

            var lotSize = symbol.LotSize;

            var price = company.Price;
            var buyPrice = tickerInfo.Prices.Buy?.Value;
            var currency = tickerInfo.Prices.Buy?.Currency ?? Currency.Rub;
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

            if (myDiffRub > 0)
            {
                totalBuyRub += myDiffRub;
                totalBuyCount++;
            }

            var model = new MyStock(symbol.Ticker,
            myStockCap,
            myPercent,
            myDiff,
            amountToBuy,
            myDiffRub,
            lotSize,
            lotPrice,
            price,
            currency,
            symbol.Isin,
            tickerInfo.ExchangeStatus,
            tickerInfo.IsLowLiquid,
            tickerInfo.RiskCategory,
            tickerInfo.Reliable,
            tickerInfo.Rate,
            withoutBuyPrice,
            agg.moex.listing);

            models.Add(new ViewModel(company, model));
        }
        catch (Exception exc)
        {
            throw new Exception(agg.company.Ticker, exc);
        }
    }

    return new ViewModels(models, new Total(myTotalCap, totalBuyRub, totalBuyCount));
}

static void PrintViewModels(IEnumerable<ViewModel> models, Total total, string title)
{
    Console.WriteLine(title);
    Console.WriteLine();

    File.AppendAllText("output.txt", title + "\n\n");



    foreach (var model in models)
    {
        try
        {
            var company = model.company;
            var myStock = model.myStock;

            var amountToBuy = myStock.amountToBuy;
            var amountToBuyText = amountToBuy > 0 ?
            amountToBuy >= 1000 ?
             amountToBuy >= 1000000 ? $"{amountToBuy / 1000000:0}M" : $"{amountToBuy / 1000:0}K" :
              $"{amountToBuy:0}" : "--";

            var lotSize = myStock.lotSize;
            var lotSizeText = lotSize != 1 ?
            lotSize >= 1000 ?
             lotSize >= 1000000 ? $"{lotSize / 1000000:0}M" : $"{lotSize / 1000:0}K" :
              $"{lotSize:0}" : "    ";

            if (lotSizeText.Length <= 3)
            {
                lotSizeText += "  ";
            }


            var price = myStock.price;
            var priceText = lotSize != 1 ? $"{price / 1000:0.000}" : "    ";
            var lotPriceText = $"{price * lotSize / 1000:0.000}";

            var myDiffRub = myStock.myDiffRub;
            var myDiffRubText = myDiffRub / 100 < 1 && myDiffRub / 100 > -1 ? $"{myDiffRub:+0;-0}  " : $"{myDiffRub:+0;-0}" != "-+0" ? $"{myDiffRub:+0;-0}" : "    ";

            var notRusIsinText = myStock.isin.StartsWith("RU") ? "    " : "NotRu";
            var isLowLiquidText = myStock.isLowLiquid == false ? "    " : "BadLiq";
            var reliableText = myStock.reliable == true ? "    " : "BadRel";
            var riskCategoryText = myStock.riskCategory == 0 ? "     " : $"Risk{myStock.riskCategory}";

            var exchangeStatusText = myStock.exchangeStatus == "Open" ? "    " : myStock.exchangeStatus;
            if (myStock.withoutBuyPrice)
            {
                exchangeStatusText += "!";
            }
            var currencyText = myStock.currency == Currency.Rub ? "   " : myStock.currency.ToString();

            var listingText = myStock.listing == 1 ? "     " : $"List{myStock.listing}";

            var changeMonth = company.changeMonth;
            var changeMonthText = changeMonth == "0" ? "0   " : changeMonth == string.Empty ? "     " : changeMonth;

            var changeYear = company.changeYear;
            var changeYearText = changeYear == "0" ? "0   " : changeYear == string.Empty ? "     " : changeYear;

            var line = $"{company.Index}\t{company.NewPercent:P2}\t\t{myStock.myPercent:P2}\t{myStock.myDiff:+0.00%;-0.00%}\t{myStock.myStockCap / 1000:0}\t\t{company.Percent:P2}\t{company.PercentDiff:+0.00%;-0.00%}\t\t{company.Cap:0.00}\t{changeYearText}\t{changeMonthText}\t{exchangeStatusText}\thttps://www.tinkoff.ru/invest/stocks/{myStock.ticker}\t{amountToBuyText}\t{myDiffRubText}\t{lotPriceText}\t{priceText}\t{lotSizeText}\t{myStock.isin}\t{notRusIsinText}\t{currencyText}\t{isLowLiquidText}\t{listingText}\t{riskCategoryText}\t{reliableText}\t{company.Title}";
            Console.WriteLine(line);
            File.AppendAllText("output.txt", line + "\n");
        }
        catch (Exception exc)
        {
            throw new Exception(model.company.Ticker, exc);
        }
    }


    var totalCapMessage = $"\nMy total cap: {total.myTotalCap / 1000000:0.000}";
    Console.Write(totalCapMessage);
    File.AppendAllText("output.txt", totalCapMessage);

    var totalBuyRubMessage = $"\nMy total buy: {total.totalBuyRub / 1000:0}K ({total.totalBuyCount}шт)";
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

public record Company(int Index, string Title, string Ticker, double Cap, double Price, double Percent, double NewPercent, double PercentDiff, string changeMonth, string changeYear);

public record Aggregate(Company company, CurrencyElement myStock, Payload tickerInfo, Moex moex);

public record ViewModel(Company company, MyStock myStock);

public record ViewModels(List<ViewModel> models, Total total);

public record MyStock(string ticker, double myStockCap, double myPercent, double myDiff, double amountToBuy, double myDiffRub, long lotSize, double lotPrice, double price, Currency currency, string isin, string exchangeStatus, bool isLowLiquid, long riskCategory, bool reliable, long rate, bool withoutBuyPrice, int listing);

public record Total(double myTotalCap, double totalBuyRub, int totalBuyCount);

public record Moex(int listing);

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