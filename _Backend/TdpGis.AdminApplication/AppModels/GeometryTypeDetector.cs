using System.Text.Json;
using TdpGis.Domain;

namespace TdpGis.AdminApplication.AppModels;

/// <summary>
///     Infers the exact geometry type from a sample document/row (WKT or GeoJSON).
/// </summary>
public static class GeometryTypeDetector
{
    private static readonly HashSet<string> GeometryPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "geom", "geometry", "shape", "wkt", "the_geom", "wkb_geometry", "geog", "geography"
    };

    /// <summary>Longer tokens first so MULTIPOLYGON wins over POLYGON, etc.</summary>
    private static readonly (string Token, GeometryType Type)[] WktTokens =
    [
        ("GEOMETRYCOLLECTION", GeometryType.GeometryCollection),
        ("MULTIPOLYGON", GeometryType.MultiPolygon),
        ("MULTILINESTRING", GeometryType.MultiLineString),
        ("MULTIPOINT", GeometryType.MultiPoint),
        ("LINESTRING", GeometryType.LineString),
        ("POLYGON", GeometryType.Polygon),
        ("POINT", GeometryType.Point)
    ];

    private static readonly Dictionary<string, GeometryType> GeoJsonTypes =
        WktTokens.ToDictionary(t => t.Token, t => t.Type, StringComparer.OrdinalIgnoreCase);

    public static GeometryType? DetectFromSampleJson(string? sampleJson)
    {
        if (string.IsNullOrWhiteSpace(sampleJson) || sampleJson is "{}" or "null")
            return null;

        try
        {
            using var doc = JsonDocument.Parse(sampleJson);
            return DetectFromGeometryFields(doc.RootElement)
                   ?? DetectFromElement(doc.RootElement)
                   ?? DetectFromWktPrefix(sampleJson);
        }
        catch (JsonException)
        {
            return DetectFromWktPrefix(sampleJson);
        }
    }

    private static GeometryType? DetectFromGeometryFields(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (!GeometryPropertyNames.Contains(property.Name))
                continue;

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                var fromWkt = DetectFromWktPrefix(property.Value.GetString());
                if (fromWkt.HasValue)
                    return fromWkt;
            }

            var nested = DetectFromElement(property.Value);
            if (nested.HasValue)
                return nested;
        }

        return null;
    }

    private static GeometryType? DetectFromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                if (element.TryGetProperty("type", out var typeProp) &&
                    typeProp.ValueKind == JsonValueKind.String &&
                    GeoJsonTypes.TryGetValue(typeProp.GetString() ?? string.Empty, out var mapped))
                    return mapped;

                if (element.TryGetProperty("geometry", out var geometryProp))
                {
                    var nestedGeometry = DetectFromElement(geometryProp);
                    if (nestedGeometry.HasValue)
                        return nestedGeometry;
                }

                foreach (var property in element.EnumerateObject())
                {
                    var nested = DetectFromElement(property.Value);
                    if (nested.HasValue)
                        return nested;
                }

                break;
            }
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = DetectFromElement(item);
                    if (nested.HasValue)
                        return nested;
                }

                break;
            case JsonValueKind.String:
                return DetectFromWktPrefix(element.GetString());
        }

        return null;
    }

    /// <summary>
    ///     Match WKT at the start of the value (optional EWKT <c>SRID=…;</c> prefix).
    /// </summary>
    private static GeometryType? DetectFromWktPrefix(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var upper = text.TrimStart().ToUpperInvariant();
        if (upper.StartsWith("SRID=", StringComparison.Ordinal))
        {
            var semi = upper.IndexOf(';');
            if (semi < 0)
                return null;
            upper = upper[(semi + 1)..].TrimStart();
        }

        foreach (var (token, type) in WktTokens)
        {
            if (!upper.StartsWith(token, StringComparison.Ordinal))
                continue;

            // Allow "POLYGON (" / "POLYGON Z (" but not matching inside longer tokens.
            if (upper.Length > token.Length && char.IsLetterOrDigit(upper[token.Length]))
                continue;

            return type;
        }

        return null;
    }
}