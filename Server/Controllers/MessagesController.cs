using Microsoft.AspNetCore.Mvc;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;

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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] MessageModel message)
        {
            await _messageService.SendMessage(message);
            return Ok();
        }
    }
}
