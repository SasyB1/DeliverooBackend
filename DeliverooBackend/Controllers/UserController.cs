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
                if (ex.Message.Contains("Email già registrata"))
                {
                    return Conflict(new { message = "L'email è già registrata." });
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Errore interno del server: {ex.Message}" });
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
                    bool isUserDeleted = _utenteService.IsUserDeleted(request.Email);
                    if (isUserDeleted)
                    {
                        return Unauthorized(new { message = "L'account è stato eliminato." });
                    }
                    return Unauthorized(new { message = "Invalid credentials." });
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



        [HttpPut("update/{id}")]
        public IActionResult UpdateUser(int id, [FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { message = "Richiesta non valida." });
            }

           
            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "La password è obbligatoria." });
            }

            try
            {
               
                bool success = _utenteService.UpdateUser(id, request.Nome, request.Cognome, request.Telefono, request.Email, request.Ruolo, request.Password);

                if (success)
                {
                   
                    var updatedUser = _utenteService.GetUserById(id); 
                    if (updatedUser == null)
                    {
                        return NotFound(new { message = "Utente non trovato." });
                    }

                    
                    return Ok(updatedUser);
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Modifica utente fallita." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Errore interno del server: {ex.Message}" });
            }
        }






        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                bool success = await _utenteService.DeleteUser(id);

                if (success)
                {
                    return Ok(new { message = "Utente eliminato con successo e tutti i dati correlati sono stati marcati come cancellati." });
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Eliminazione utente fallita." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Errore interno del server: {ex.Message}" });
            }
        }

    }
}
