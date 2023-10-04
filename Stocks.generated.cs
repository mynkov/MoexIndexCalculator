namespace QuickType
{
    using System;
    using System.Collections.Generic;

    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Globalization;

    public partial class Stocks
    {
        [JsonPropertyName("currencies")]
        public List<CurrencyElement> Currencies { get; set; }

        [JsonPropertyName("instruments")]
        public List<CurrencyElement> Instruments { get; set; }

        [JsonPropertyName("issuers")]
        public List<CurrencyElement> Issuers { get; set; }

        [JsonPropertyName("sectors")]
        public List<CurrencyElement> Sectors { get; set; }

        [JsonPropertyName("totalAmountCurrency")]
        public TotalAmountCurrency TotalAmountCurrency { get; set; }

        [JsonPropertyName("totalAmountCurrencyQty")]
        public long TotalAmountCurrencyQty { get; set; }

        [JsonPropertyName("totalAmountInstrument")]
        public TotalAmountCurrency TotalAmountInstrument { get; set; }

        [JsonPropertyName("totalAmountInstrumentQty")]
        public long TotalAmountInstrumentQty { get; set; }

        [JsonPropertyName("totalAmountIssuer")]
        public TotalAmountCurrency TotalAmountIssuer { get; set; }

        [JsonPropertyName("totalAmountIssuerQty")]
        public long TotalAmountIssuerQty { get; set; }

        [JsonPropertyName("totalAmountSector")]
        public TotalAmountCurrency TotalAmountSector { get; set; }

        [JsonPropertyName("totalAmountSectorQty")]
        public long TotalAmountSectorQty { get; set; }
    }

    public partial class CurrencyElement
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("instrumentInfo")]
        public List<InstrumentInfo> InstrumentInfo { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("valueAbsolute")]
        public TotalAmountCurrency ValueAbsolute { get; set; }

        [JsonPropertyName("valueRelative")]
        public double ValueRelative { get; set; }
    }

    public partial class InstrumentInfo
    {
        [JsonPropertyName("positionType")]
        public PositionType PositionType { get; set; }

        [JsonPropertyName("positionUid")]
        public Guid PositionUid { get; set; }

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; }
    }

    public partial class TotalAmountCurrency
    {
        [JsonPropertyName("currency")]
        public CurrencyEnum Currency { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }
    }

    public enum PositionType { Etf, Stock, Currency, Bond };

    public enum CurrencyEnum { Rub };

    internal static class Converter
    {
        public static readonly JsonSerializerOptions Settings = new(JsonSerializerDefaults.General)
        {
            Converters =
            {
                PositionTypeConverter.Singleton,
                CurrencyEnumConverter.Singleton,
                new DateOnlyConverter(),
                new TimeOnlyConverter(),
                IsoDateTimeOffsetConverter.Singleton
            },
        };
    }

    internal class PositionTypeConverter : JsonConverter<PositionType>
    {
        public override bool CanConvert(Type t) => t == typeof(PositionType);

        public override PositionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            switch (value)
            {
                case "etf":
                    return PositionType.Etf;
                case "stock":
                    return PositionType.Stock;
                case "currency":
                    return PositionType.Currency;  
                case "bond":
                    return PositionType.Bond;  
            }
            throw new Exception($"Cannot unmarshal type PositionType {value}");
        }

        public override void Write(Utf8JsonWriter writer, PositionType value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case PositionType.Etf:
                    JsonSerializer.Serialize(writer, "etf", options);
                    return;
                case PositionType.Stock:
                    JsonSerializer.Serialize(writer, "stock", options);
                    return;
                case PositionType.Currency:
                    JsonSerializer.Serialize(writer, "currency", options);
                    return;
                case PositionType.Bond:
                    JsonSerializer.Serialize(writer, "bond", options);
                    return;
            }
            throw new Exception("Cannot marshal type PositionType");
        }

        public static readonly PositionTypeConverter Singleton = new PositionTypeConverter();
    }

    internal class CurrencyEnumConverter : JsonConverter<CurrencyEnum>
    {
        public override bool CanConvert(Type t) => t == typeof(CurrencyEnum);

        public override CurrencyEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == "RUB")
            {
                return CurrencyEnum.Rub;
            }
            throw new Exception("Cannot unmarshal type CurrencyEnum");
        }

        public override void Write(Utf8JsonWriter writer, CurrencyEnum value, JsonSerializerOptions options)
        {
            if (value == CurrencyEnum.Rub)
            {
                JsonSerializer.Serialize(writer, "RUB", options);
                return;
            }
            throw new Exception("Cannot marshal type CurrencyEnum");
        }

        public static readonly CurrencyEnumConverter Singleton = new CurrencyEnumConverter();
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