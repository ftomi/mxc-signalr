using MongoDB.Driver;
using SignalRChatAppBackend.Models;

namespace SignalRChatAppBackend.Services;

public class UserService
{
    private readonly IMongoCollection<User> _messages;

    public UserService(IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase("ChatApp");
        _messages = database.GetCollection<User>("Users");
    }
    
    // save user if name is not present else update the user id
    public async Task SaveUserAsync(User user)
    {
        var existingUser = await GetUserByNameAsync(user.Username);
        if (existingUser == null)
        {
            await _messages.InsertOneAsync(user);
        }
        else
        {
            var filter = Builders<User>.Filter.Eq(u => u.Username, existingUser.Username);
            var update = Builders<User>.Update
                .Set(u => u.Id, user.Id);
            await _messages.UpdateOneAsync(filter, update);
        }
    }
    
    // Update user in MongoDB
    public async Task UpdateUserAsync(string id, User user)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, id);
        var update = Builders<User>.Update
            .Set(u => u.Username, user.Username);
        await _messages.UpdateOneAsync(filter, update);
    }
    
    // Get user by Name
    public async Task<User> GetUserByNameAsync(string name)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Username, name);
        return await _messages.Find(filter).FirstOrDefaultAsync();
    }
}