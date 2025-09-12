using Microsoft.AspNetCore.Mvc;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;
using System;
using System.Collections.Generic;

namespace NDAProcesses.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("{userName}")]
        public async Task<IEnumerable<MessageModel>> Get(string userName)
        {
            return await _messageService.GetMessages(userName);
        }

        [HttpGet("{userName}/conversation/{recipient}")]
        public async Task<IEnumerable<MessageModel>> GetConversation(string userName, string recipient)
        {
            return await _messageService.GetConversation(userName, recipient);
        }

        [HttpGet("{userName}/recipients")]
        public async Task<IEnumerable<string>> GetRecipients(string userName)
        {
            return await _messageService.GetRecipients(userName);
        }

        [HttpGet("contacts")]
        public async Task<IEnumerable<ContactModel>> GetContacts()
        {
            return await _messageService.GetContacts();
        }

        [HttpPost("contacts")]
        public async Task<IActionResult> SaveContact([FromBody] ContactModel contact)
        {
            try
            {
                await _messageService.SaveContact(contact);
                return Ok();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("contacts/{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            await _messageService.DeleteContact(id);
            return Ok();
        }

        [HttpGet("{userName}/scheduled")]
        public async Task<IEnumerable<ScheduledMessageModel>> GetScheduled(string userName)
        {
            return await _messageService.GetScheduledMessages(userName);
        }

        [HttpPost("schedule")]
        public async Task<IActionResult> Schedule([FromBody] ScheduledMessageModel message)
        {
            await _messageService.ScheduleMessage(message);
            return Ok();
        }

        [HttpDelete("{userName}/scheduled/{id}")]
        public async Task<IActionResult> Cancel(string userName, int id)
        {
            await _messageService.CancelScheduledMessage(id, userName);
            return Ok();
        }

        [HttpGet("{userName}/readstates")]
        public async Task<Dictionary<string, DateTime>> GetReadStates(string userName)
        {
            return await _messageService.GetReadStates(userName);
        }

        public class ReadRequest
        {
            public string Recipient { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        [HttpPost("{userName}/read")]
        public async Task<IActionResult> MarkRead(string userName, [FromBody] ReadRequest request)
        {
            await _messageService.MarkRead(userName, request.Recipient, request.Timestamp);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MessageModel message)
        {
            await _messageService.SendMessage(message);
            return Ok();
        }
    }
}
