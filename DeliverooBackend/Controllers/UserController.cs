using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DeliverooBackend.Services;
using DeliverooBackend.Models;

namespace DeliverooBackend.Controllers
{
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UtenteService _utenteService;

        public UserController(UtenteService utenteService)
        {
            _utenteService = utenteService;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid user data.");
            }

            try
            {
                bool success = _utenteService.Register(
                    request.Nome,
                    request.Cognome,
                    request.Email,
                    request.Telefono,
                    request.Indirizzo,
                    request.Password,
                    request.Ruolo
                );

                if (success)
                {
                    return Ok("User registered successfully.");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "User registration failed.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }
    }
}


