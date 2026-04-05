using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using TdpGis.Domain;

namespace TdpGis.Infrastructure.Helpers;

public static class OutputMapping
{
    public static JObject ConvertFromBson(BsonDocument doc, List<PropertyMapping> maps)
    {
        var jo = new JObject();
        try
        {
            foreach (var prop in maps)
                switch (prop.ColumnType)
                {
                    case PropertyType.Normal:
                        jo.Add(prop.PropertyLabel, doc.GetValue(prop.PropertyName).ToString());
                        break;

                    case PropertyType.Object:
                        //work around the problem due to JObject parse BsonDocument ToJson function
                        var currentElement = JObject.Parse(doc.GetElement(prop.PropertyName).ToJson());
                        jo.Add(prop.PropertyLabel,
                            currentElement["Value"] == null ? new JObject() : currentElement["Value"] as JObject);
                        break;
                }
        }
        catch (Exception ex)
        {
            // ignored
        }

        return jo;
    }
}