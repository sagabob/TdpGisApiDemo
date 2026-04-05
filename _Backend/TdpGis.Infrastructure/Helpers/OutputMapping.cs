using System.Text.Json.Nodes;
using MongoDB.Bson;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Helpers;

public static class OutputMapping
{
    public static JsonObject ConvertFromBson(BsonDocument doc, List<PropertyMapping> maps)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(maps);

        var jo = new JsonObject();
        foreach (var prop in maps)
        {
            if (string.IsNullOrEmpty(prop.PropertyName))
                continue;

            if (!doc.TryGetValue(prop.PropertyName, out var value))
                continue;

            switch (prop.ColumnType)
            {
                case PropertyType.Normal:
                    jo[prop.PropertyLabel] = NormalToJsonNode(value);
                    break;

                case PropertyType.Object:
                    jo[prop.PropertyLabel] = value.IsBsonNull ? JsonNull() : BsonValueToJsonNode(value);
                    break;
            }
        }

        return jo;
    }

    /// <summary>
    ///     Normal columns are exposed as JSON string values (legacy behavior of <see cref="BsonValue.ToString" />).
    /// </summary>
    private static JsonNode NormalToJsonNode(BsonValue value)
    {
        return value.IsBsonNull ? JsonNull() : JsonValue.Create(value.ToString())!;
    }

    private static JsonNode JsonNull()
    {
        return JsonValue.Create((object?)null)!;
    }

    private static JsonNode BsonValueToJsonNode(BsonValue value)
    {
        if (value.IsBsonNull)
            return JsonNull();

        switch (value.BsonType)
        {
            case BsonType.Array:
            {
                var arr = new JsonArray();
                foreach (var item in value.AsBsonArray)
                    arr.Add(BsonValueToJsonNode(item));
                return arr;
            }
            case BsonType.Document:
            {
                var obj = new JsonObject();
                foreach (var el in value.AsBsonDocument.Elements)
                    obj[el.Name] = BsonValueToJsonNode(el.Value);
                return obj;
            }
            case BsonType.Boolean:
                return JsonValue.Create(value.AsBoolean)!;
            case BsonType.DateTime:
                return JsonValue.Create(value.ToUniversalTime())!;
            case BsonType.Int32:
                return JsonValue.Create(value.AsInt32)!;
            case BsonType.Int64:
                return JsonValue.Create(value.AsInt64)!;
            case BsonType.Double:
                return JsonValue.Create(value.AsDouble)!;
            case BsonType.Decimal128:
                return JsonValue.Create((decimal)value.AsDecimal128)!;
            case BsonType.String:
                return JsonValue.Create(value.AsString)!;
            case BsonType.ObjectId:
                return JsonValue.Create(value.AsObjectId.ToString())!;
            case BsonType.Binary:
                return JsonValue.Create(Convert.ToBase64String(value.AsBsonBinaryData.Bytes))!;
            default:
                return JsonValue.Create(value.ToString())!;
        }
    }
}