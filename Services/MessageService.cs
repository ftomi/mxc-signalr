
using MongoDB.Driver;
using SignalRChatAppBackend.Models;

namespace SignalRChatAppBackend.Services;

public class MessageService
{
    private readonly IMongoCollection<Message> _messages;

    public MessageService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("ChatApp");
        _messages = database.GetCollection<Message>("Messages");
    }

    public async Task SaveMessageAsync(Message message)
    {
        await _messages.InsertOneAsync(message);
    }

    public async Task<List<Message>> GetMessagesAsync()
    {
        return await _messages.Find(
            _ => true // x.IsDeleted == false
            )
            .ToListAsync();
    }
    
    public async Task<List<Message>> GetMessagesByChannelAsync(string channel, string? userName = null)
    {
        if (userName != null)
        {
            return await _messages.Find(
                    m => m.Channel == channel && m.User == "System" && m.Text.StartsWith($"{userName}") // && m.IsDeleted == false
                )
                .ToListAsync();
        }
        return await _messages.Find(
                m => m.Channel == channel // && m.IsDeleted == false
                )
            .ToListAsync();
    }
    
    public async Task DeleteMessagesByChannelAsync(string channel)
    {
        // Delete all messages in the specified channel
        await _messages.DeleteManyAsync(m => m.Channel == channel);
    }
    
    public async Task SoftDeleteMessagesByChannelAsync(string channel)
    {
        // Soft delete all messages in the specified channel
        var filter = Builders<Message>.Filter.Eq(m => m.Channel, channel);
        var update = Builders<Message>.Update.Set(m => m.IsDeleted, true);
        await _messages.UpdateManyAsync(filter, update);
    }
    
    public async Task<List<Message>> GetDirectMessagesAsync(string fromUser, string toUser)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.User, fromUser),
            Builders<Message>.Filter.Eq(m => m.Recipient, toUser) // ,
            // Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        return await _messages.Find(filter).ToListAsync();
    }
    
    public async Task<List<string>> GetUsersAsync()
    {
        var users = await _messages.Distinct(
                m => m.User, x => x.User != "System" // x.IsDeleted == false
                )
            .ToListAsync();
        return users;
    }
}