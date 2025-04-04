using Microsoft.AspNetCore.SignalR;
using SignalRChatAppBackend.Models;
using SignalRChatAppBackend.Services;

namespace SignalRChatAppBackend.Hubs;
public class ChatHub : Hub
{
    private readonly MessageService _messageService;

    public ChatHub(MessageService messageService)
    {
        _messageService = messageService;
    }

    public async Task SendMessage(string user, string message, string channel)
    {
        var msg = new Message
        {
            User = user,
            Text = message,
            Timestamp = DateTime.UtcNow,
            Channel = channel
        };

        // Save message to MongoDB
        await _messageService.SaveMessageAsync(msg);

        // Send message to all clients in the specified channel
        await Clients.Group(channel).SendAsync("ReceiveMessage", user, message, channel);
    }

    public async Task JoinChannel(string channel)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channel);
        
        // IMPROVEMENT: Broadcast to all clients in the channel that a new user has joined
        // This can be used to update the UI or notify other users
        
        await _messageService.SaveMessageAsync(new Message
        {
            User = "System",
            Text = $"{Context.ConnectionId} has joined the channel.",
            Timestamp = DateTime.UtcNow,
            Channel = channel
        });
        
        await Clients.Group(channel).SendAsync("UserJoined", Context.ConnectionId, channel);
        
    }

    public async Task LeaveChannel(string channel)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
    }
    
    public async Task CreateChannel(string channelName, string systemUser, string welcomeText)
    {
        // Add the system user to the new channel
        await Groups.AddToGroupAsync(Context.ConnectionId, channelName);

        // Create a welcome message
        var welcomeMessage = new Message
        {
            User = systemUser,
            Text = string.IsNullOrEmpty(welcomeText) ? $"Welcome to the {channelName} channel!" : welcomeText,
            Timestamp = DateTime.UtcNow,
            Channel = channelName
        };

        // Save the welcome message to MongoDB
        await _messageService.SaveMessageAsync(welcomeMessage);

        // Send the welcome message to all clients in the new channel
        await Clients.Group(channelName).SendAsync("ReceiveMessage", systemUser, welcomeText, channelName);
        await Clients.Group(channelName).SendAsync("ChannelCreated", channelName);
    }
    public async Task DeleteChannel(string channelName)
    {
        // Delete all messages in the channel
        await _messageService.DeleteMessagesByChannelAsync(channelName);
        
        // Notify clients that the channel has been deleted
        await Clients.Group(channelName).SendAsync("ChannelDeleted", channelName);
    }
}