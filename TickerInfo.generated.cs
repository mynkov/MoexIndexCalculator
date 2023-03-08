namespace QuickTypeTicker
{
    using System;
    using System.Collections.Generic;

    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Globalization;

    public partial class TickerInfo
    {
        [JsonPropertyName("payload")]
        public Payload Payload { get; set; }

        [JsonPropertyName("trackingId")]
        public string TrackingId { get; set; }

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public partial class Payload
    {
        [JsonPropertyName("symbol")]
        public Symbol Symbol { get; set; }

        [JsonPropertyName("prices")]
        public Prices Prices { get; set; }

        [JsonPropertyName("price")]
        public LotPrice Price { get; set; }

        [JsonPropertyName("lotPrice")]
        public LotPrice LotPrice { get; set; }

        [JsonPropertyName("earnings")]
        public Earnings Earnings { get; set; }

        [JsonPropertyName("exchangeStatus")]
        public string ExchangeStatus { get; set; }

        [JsonPropertyName("instrumentStatus")]
        public string InstrumentStatus { get; set; }

        [JsonPropertyName("historyStartDate")]
        public DateTimeOffset HistoryStartDate { get; set; }

        [JsonPropertyName("historicalPrices")]
        public List<HistoricalPrice> HistoricalPrices { get; set; }

        [JsonPropertyName("contentMarker")]
        public ContentMarker ContentMarker { get; set; }

        [JsonPropertyName("isFavorite")]
        public bool IsFavorite { get; set; }

        [JsonPropertyName("riskCategory")]
        public long RiskCategory { get; set; }

        [JsonPropertyName("availableOrders")]
        public List<string> AvailableOrders { get; set; }

        [JsonPropertyName("limitUp")]
        public double LimitUp { get; set; }

        [JsonPropertyName("limitDown")]
        public double LimitDown { get; set; }

        [JsonPropertyName("profitable")]
        public bool Profitable { get; set; }

        [JsonPropertyName("reliable")]
        public bool Reliable { get; set; }

        [JsonPropertyName("rate")]
        public long Rate { get; set; }

        [JsonPropertyName("isLowLiquid")]
        public bool IsLowLiquid { get; set; }

        [JsonPropertyName("isAvailableToGift")]
        public bool IsAvailableToGift { get; set; }

        [JsonPropertyName("isBlockedTradeClearingAccount")]
        public bool IsBlockedTradeClearingAccount { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

    public partial class ContentMarker
    {
        [JsonPropertyName("news")]
        public bool News { get; set; }

        [JsonPropertyName("ideas")]
        public bool Ideas { get; set; }

        [JsonPropertyName("dividends")]
        public bool Dividends { get; set; }

        [JsonPropertyName("prognosis")]
        public bool Prognosis { get; set; }

        [JsonPropertyName("events")]
        public bool Events { get; set; }

        [JsonPropertyName("fundamentals")]
        public bool Fundamentals { get; set; }

        [JsonPropertyName("recalibration")]
        public bool Recalibration { get; set; }

        [JsonPropertyName("coupons")]
        public bool Coupons { get; set; }
    }

    public partial class Earnings
    {
        [JsonPropertyName("absolute")]
        public Absolute Absolute { get; set; }

        [JsonPropertyName("relative")]
        public double Relative { get; set; }
    }

    public partial class Absolute
    {
        [JsonPropertyName("currency")]
        public Currency Currency { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    public partial class HistoricalPrice
    {
        [JsonPropertyName("amount")]
        public double Amount { get; set; }

        [JsonPropertyName("time")]
        public DateTimeOffset Time { get; set; }

        [JsonPropertyName("unixtime")]
        public long Unixtime { get; set; }

        [JsonPropertyName("earningsInfo")]
        public Earnings EarningsInfo { get; set; }
    }

    public partial class LotPrice
    {
        [JsonPropertyName("currency")]
        public Currency Currency { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("fromCache")]
        public bool FromCache { get; set; }
    }

    public partial class Prices
    {
        [JsonPropertyName("buy")]
        public LotPrice Buy { get; set; }

        [JsonPropertyName("sell")]
        public LotPrice Sell { get; set; }

        [JsonPropertyName("last")]
        public LotPrice Last { get; set; }

        [JsonPropertyName("close")]
        public LotPrice Close { get; set; }

        [JsonPropertyName("auction")]
        public LotPrice Auction { get; set; }
    }

    public partial class Symbol
    {
        [JsonPropertyName("ticker")]
        public string Ticker { get; set; }

        [JsonPropertyName("symbolType")]
        public string SymbolType { get; set; }

        [JsonPropertyName("classCode")]
        public string ClassCode { get; set; }

        [JsonPropertyName("bcsClassCode")]
        public string BcsClassCode { get; set; }

        [JsonPropertyName("isin")]
        public string Isin { get; set; }

        [JsonPropertyName("currency")]
        public Currency Currency { get; set; }

        [JsonPropertyName("lotSize")]
        public long LotSize { get; set; }

        [JsonPropertyName("minPriceIncrement")]
        public double MinPriceIncrement { get; set; }

        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }

        [JsonPropertyName("exchangeShowName")]
        public string ExchangeShowName { get; set; }

        [JsonPropertyName("exchangeLogoUrl")]
        public Uri ExchangeLogoUrl { get; set; }

        [JsonPropertyName("sessionOpen")]
        public DateTimeOffset SessionOpen { get; set; }

        [JsonPropertyName("sessionClose")]
        public DateTimeOffset SessionClose { get; set; }

        [JsonPropertyName("showName")]
        public string ShowName { get; set; }

        [JsonPropertyName("logoName")]
        public string LogoName { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; }

        [JsonPropertyName("textColor")]
        public string TextColor { get; set; }

        [JsonPropertyName("sector")]
        public string Sector { get; set; }

        [JsonPropertyName("countryOfRiskBriefName")]
        public string CountryOfRiskBriefName { get; set; }

        [JsonPropertyName("countryOfRiskLogoUrl")]
        public Uri CountryOfRiskLogoUrl { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }

        [JsonPropertyName("blackout")]
        public bool Blackout { get; set; }

        [JsonPropertyName("noTrade")]
        public bool NoTrade { get; set; }

        [JsonPropertyName("marketStartTime")]
        public string MarketStartTime { get; set; }

        [JsonPropertyName("marketEndTime")]
        public string MarketEndTime { get; set; }

        [JsonPropertyName("brokerAccountTypesList")]
        public List<string> BrokerAccountTypesList { get; set; }

        [JsonPropertyName("timeToOpen")]
        public long TimeToOpen { get; set; }

        [JsonPropertyName("isOTC")]
        public bool IsOtc { get; set; }

        [JsonPropertyName("bbGlobal")]
        public string BbGlobal { get; set; }

        [JsonPropertyName("shortIsEnabled")]
        public bool ShortIsEnabled { get; set; }

        [JsonPropertyName("longIsEnabled")]
        public bool LongIsEnabled { get; set; }

        [JsonPropertyName("isPrivate")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("tradeQualInvestor")]
        public bool TradeQualInvestor { get; set; }

        [JsonPropertyName("longLeverageSize")]
        public double LongLeverageSize { get; set; }

        [JsonPropertyName("shortLeverageSize")]
        public double ShortLeverageSize { get; set; }

        [JsonPropertyName("securityUids")]
        public SecurityUids SecurityUids { get; set; }
    }

    public partial class SecurityUids
    {
        [JsonPropertyName("positionUid")]
        public Guid PositionUid { get; set; }

        [JsonPropertyName("instrumentUid")]
        public Guid InstrumentUid { get; set; }
    }

    public enum Currency { Rub };

    internal static class Converter
    {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
        {
            Converters =
            {
                CurrencyConverter.Singleton,
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton
            },
        };
    }

    internal class CurrencyConverter : JsonConverter<Currency>
    {
        public override bool CanConvert(Type t) => t == typeof(Currency);

        public override Currency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == "RUB")
            {
                return Currency.Rub;
            }
            throw new Exception("Cannot unmarshal type Currency");
        }

        public override void Write(Utf8JsonWriter writer, Currency value, JsonSerializerOptions options)
        {
            if (value == Currency.Rub)
            {
                JsonSerializer.Serialize(writer, "RUB", options);
                return;
            }
            throw new Exception("Cannot marshal type Currency");
        }

        public static readonly CurrencyConverter Singleton = new CurrencyConverter();
    }
    
    public class DateOnlyConverter : JsonConverter<DateOnly>
    {
        private readonly string serializationFormat;
        public DateOnlyConverter() : this(null) { }

        public DateOnlyConverter(string? serializationFormat)
        {
            this.serializationFormat = serializationFormat ?? "yyyy-MM-dd";
        }

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return DateOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    public class TimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private readonly string serializationFormat;

        public TimeOnlyConverter() : this(null) { }

        public TimeOnlyConverter(string? serializationFormat)
        {
            this.serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
        }

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            return TimeOnly.Parse(value!);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(serializationFormat));
    }

    internal class IsoDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override bool CanConvert(Type t) => t == typeof(DateTimeOffset);

        private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

        private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
        private string? _dateTimeFormat;
        private CultureInfo? _culture;

        public DateTimeStyles DateTimeStyles
        {
            get => _dateTimeStyles;
            set => _dateTimeStyles = value;
        }

        public string? DateTimeFormat
        {
            get => _dateTimeFormat ?? string.Empty;
            set => _dateTimeFormat = (string.IsNullOrEmpty(value)) ? null : value;
        }

        public CultureInfo Culture
        {
            get => _culture ?? CultureInfo.CurrentCulture;
            set => _culture = value;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            string text;


            if ((_dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal
                || (_dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
            {
                value = value.ToUniversalTime();
            }

            text = value.ToString(_dateTimeFormat ?? DefaultDateTimeFormat, Culture);

            writer.WriteStringValue(text);
        }

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? dateText = reader.GetString();

            if (string.IsNullOrEmpty(dateText) == false)
            {
                if (!string.IsNullOrEmpty(_dateTimeFormat))
                {
                    return DateTimeOffset.ParseExact(dateText, _dateTimeFormat, Culture, _dateTimeStyles);
                }
                else
                {
                    return DateTimeOffset.Parse(dateText, Culture, _dateTimeStyles);
                }
            }
            else
            {
                return default(DateTimeOffset);
            }
        }


        public static readonly IsoDateTimeOffsetConverter Singleton = new IsoDateTimeOffsetConverter();
    }
}
