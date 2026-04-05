using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Helpers;

public static class OutputMapping
{
    public static JObject ConvertFromBson(BsonDocument doc, List<PropertyMapping> maps)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(maps);

        var jo = new JObject();
        foreach (var prop in maps)
        {
            if (string.IsNullOrEmpty(prop.PropertyName))
                continue;

            if (!doc.TryGetValue(prop.PropertyName, out var value))
                continue;

            switch (prop.ColumnType)
            {
                case PropertyType.Normal:
                    jo[prop.PropertyLabel] = NormalToJToken(value);
                    break;

                case PropertyType.Object:
                    jo[prop.PropertyLabel] = value.IsBsonNull ? JValue.CreateNull() : BsonValueToJToken(value);
                    break;
            }
        }

        return jo;
    }

    /// <summary>
    ///     Normal columns are exposed as JSON string values (legacy behavior of <see cref="BsonValue.ToString" />).
    /// </summary>
    private static JToken NormalToJToken(BsonValue value) =>
        value.IsBsonNull ? JValue.CreateNull() : new JValue(value.ToString());

    private static JToken BsonValueToJToken(BsonValue value)
    {
        if (value.IsBsonNull)
            return JValue.CreateNull();

        switch (value.BsonType)
        {
            case BsonType.Array:
            {
                var arr = new JArray();
                foreach (var item in value.AsBsonArray)
                    arr.Add(BsonValueToJToken(item));
                return arr;
            }
            case BsonType.Document:
            {
                var obj = new JObject();
                foreach (var el in value.AsBsonDocument.Elements)
                    obj[el.Name] = BsonValueToJToken(el.Value);
                return obj;
            }
            case BsonType.Boolean:
                return new JValue(value.AsBoolean);
            case BsonType.DateTime:
                return new JValue(value.ToUniversalTime());
            case BsonType.Int32:
                return new JValue(value.AsInt32);
            case BsonType.Int64:
                return new JValue(value.AsInt64);
            case BsonType.Double:
                return new JValue(value.AsDouble);
            case BsonType.Decimal128:
                return new JValue((decimal)value.AsDecimal128);
            case BsonType.String:
                return new JValue(value.AsString);
            case BsonType.ObjectId:
                return new JValue(value.AsObjectId.ToString());
            case BsonType.Binary:
                return new JValue(Convert.ToBase64String(value.AsBsonBinaryData.Bytes));
            default:
                return new JValue(value.ToString());
        }
    }
}
