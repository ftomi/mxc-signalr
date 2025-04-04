
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
        return await _messages.Find(_ => true).ToListAsync();
    }
    
    public async Task<List<Message>> GetMessagesByChannelAsync(string channel)
    {
        return await _messages.Find(m => m.Channel == channel).ToListAsync();
    }
    
    public async Task DeleteMessagesByChannelAsync(string channel)
    {
        await _messages.DeleteManyAsync(m => m.Channel == channel);
    }
}