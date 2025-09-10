using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NDAProcesses.Shared.Models;
using NDAProcesses.Shared.Services;

namespace NDAProcesses.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<bool> ValidateUser([FromBody] UserModel user)
        {
            return await _userService.ValidateUser(user);
        }


        [HttpGet("{userName}")]
        public async Task<ActionResult<UserModel>> GetUserData(string userName)
        {
            var userData = await _userService.GetUserData(userName);
            if (userData == null)
            {
                return NotFound();
            }

            return Ok(userData);
        }
    }
}
