using MongoDB.Bson.Serialization.Attributes;

namespace TdpGis.LocalApi.Models;

public class Address
{
    [BsonElement("streetAddressId")] public int StreetAddressId { get; set; }

    [BsonElement("localityName")] public required string LocalityName { get; set; }

    [BsonElement("streetAddress")] public required string StreetAddress { get; set; }
}