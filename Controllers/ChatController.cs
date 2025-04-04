using Microsoft.AspNetCore.Mvc;
using SignalRChatAppBackend.Hubs;
using SignalRChatAppBackend.Models;
using SignalRChatAppBackend.Services;

namespace SignalRChatAppBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatHub _chatHub;
    private readonly MessageService _messageService;

    public ChatController(ChatHub chatHub, MessageService messageService)
    {
        _chatHub = chatHub;
        _messageService = messageService;
    }

    [HttpGet("channels")]
    public async Task<ActionResult<List<string>>> GetChannels()
    { 
        var messages = await _messageService.GetMessagesAsync(); 
        var channels = messages.Select(m => m.Channel).Distinct().ToList();
        return Ok(channels);
    }

    [HttpGet("channels/{channel}")]
    public async Task<ActionResult<List<Message>>> GetMessagesByChannel(string channel)
    {
        var messages = await _messageService.GetMessagesByChannelAsync(channel);
        return Ok(messages);
    }
    
    [HttpPost("channels")]
    public async Task<IActionResult> CreateChannel([FromBody] CreateChannelRequest request)
    {
        await _chatHub.CreateChannel(request.ChannelName, request.SystemUser, request.WelcomeText);
        return Ok();
    }
    
    [HttpDelete("channels/{channelName}")]
    public async Task<IActionResult> DeleteChannel(string channelName)
    {
        await _chatHub.DeleteChannel(channelName);
        return Ok();
    }
    
    public class CreateChannelRequest
    {
        public required string ChannelName { get; set; }
        public string SystemUser { get; set; } = "System";
        public string? WelcomeText { get; set; }
    }
}