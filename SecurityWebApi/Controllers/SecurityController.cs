using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Dto;
using Security.Application.Services;

namespace SecurityWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityService _securityService;

        public SecurityController(ISecurityService securityService)
        {
            _securityService = securityService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto model)
        {
            var result = await _securityService.Login(model.Username, model.Password);

            if (result.Success)
            {
                return Ok(new
                {
                    result
                });
            }
            else
            {
                return Unauthorized(new { result });
            }
        }
        [AllowAnonymous]
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterAplicationUserModel model)
        {
            var result = await _securityService.Register(model, false, false);

            if (result.Success)
            {
                return Ok(new
                {
                    result
                });
            }
            else
            {
                return BadRequest(new
                {
                    result
                });
               
            }
        }



    }
}
