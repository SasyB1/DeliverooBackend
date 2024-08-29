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
                return BadRequest(new { message = "Richiesta non valida." });
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
                    return Ok(new { message = "Utente registrato con successo." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Registrazione fallita." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Internal server error: {ex.Message}" });
            }
        }


        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid login data.");
            }

            try
            {
                var user = _utenteService.Login(request.Email, request.Password);

                if (user == null)
                {
                    return Unauthorized("Invalid credentials.");
                }

                return Ok(new
                {
                    user.ID_Utente,
                    user.Nome,
                    user.Cognome,
                    user.Email,
                    user.Ruolo,
                    Token = user.AccessToken,
                    TokenExpiration = user.DataScadenzaToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
            }
        }
    }

}
