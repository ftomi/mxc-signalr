using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SignalRChatAppBackend.Models;

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public required string User { get; set; }
    public required string Text { get; set; }
    public DateTime Timestamp { get; set; }
    public required string Channel { get; set; }
}
