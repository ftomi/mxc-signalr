using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using SignalRChatAppBackend.Models;
using SignalRChatAppBackend.Services;

namespace SignalRChatAppBackend.Hubs;
public class ChatHub : Hub
{
    private readonly MessageService _messageService;
    private readonly UserService _userService;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ChatHub(MessageService messageService, UserService userService)
    {
        _messageService = messageService;
        _userService = userService;
    }

    public override Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var username = httpContext?.Request.Query["username"];

        if (!string.IsNullOrEmpty(username))
        {
            _userConnections[username!] = Context.ConnectionId;
        }

        return base.OnConnectedAsync();
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
        
        // Notify all clients in the channel to refresh the user list
        await Clients.All.SendAsync("RefreshUserList");
    }

    public async Task JoinChannel(string channel, string userName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, channel);
        
        // IMPROVEMENT: Broadcast to all clients in the channel that a new user has joined
        // This can be used to update the UI or notify other users
        if(CheckIfUserIsInChannel(channel, userName).Result == false)
        {
            await _messageService.SaveMessageAsync(new Message
            {
                User = "System",
                Text = $"{userName} has joined the channel.",
                Timestamp = DateTime.UtcNow,
                Channel = channel
            });
        }
        if (!string.IsNullOrEmpty(userName))
        {
            _userConnections[userName!] = Context.ConnectionId;
        }
        // await _userService.SaveUserAsync(new User { Username = userName, Id = Context.ConnectionId });
        
        // Boradcast to all clients in the channel
        await Clients.Group(channel).SendAsync("UserJoined", Context.ConnectionId, channel);
    }

    public async Task LeaveChannel(string channel)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
    }
    
    public async Task SendDirectMessage(string fromUser, string toUser, string message)
    {
        var msg = new Message
        {
            User = fromUser,
            Text = message,
            Timestamp = DateTime.UtcNow,
            Channel = $"direct_{fromUser}_{toUser}",
            Recipient = toUser
        };

        // Save message to MongoDB
        await _messageService.SaveMessageAsync(msg);

        // var toUserId = (await _userService.GetUserByNameAsync(toUser)).Id;
        
        // Send message to the specific user
        // await Clients.User(toUser).SendAsync("ReceiveDirectMessage", fromUser, message);
        
        if (_userConnections.TryGetValue(toUser, out var connectionId))
        {
            await Clients.Client(connectionId).SendAsync("ReceiveDirectMessage", fromUser, message);
        }

        // Optional step: send a copy of the message to the sender - bug on frontend - opening a channel to herself
        // if (_userConnections.TryGetValue(fromUser, out var senderConnectionId))
        // {
        //     await Clients.Client(senderConnectionId).SendAsync("ReceiveDirectMessage", fromUser, message);
        // }
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
        await Clients.All.SendAsync("ChannelCreated", channelName);
    }
    public async Task DeleteChannel(string channelName)
    {
        // Delete all messages in the channel
        await _messageService.DeleteMessagesByChannelAsync(channelName);
        // For soft delete, uncomment the line below
        // await _messageService.SoftDeleteMessagesByChannelAsync(channelName);
        
        // Notify clients that the channel has been deleted
        await Clients.All.SendAsync("ChannelDeleted", channelName);
    }
    
    public async Task<bool> CheckIfUserIsInChannel(string channelName, string userName)
    {
        return (await _messageService.GetMessagesByChannelAsync(channelName, userName)).Count != 0;
    }
}