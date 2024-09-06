using DeliverooBackend.Models;
using DeliverooBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;

namespace DeliverooBackend.Controllers
{
    [ApiController]
    public class RistorantiController : ControllerBase
    {
        private readonly RistoranteService _ristoranteService;

        public RistorantiController(RistoranteService ristoranteService)
        {
            _ristoranteService = ristoranteService;
        }

        [HttpGet("vicini")]
        public ActionResult<List<Ristorante>> GetRistorantiVicini(decimal latitudine, decimal longitudine, decimal distanzaMassimaKm)
        {
            var ristorantiVicini = _ristoranteService.GetRistorantiVicini(latitudine, longitudine, distanzaMassimaKm);
            return Ok(ristorantiVicini);
        }

        [HttpPost("crea-ristorante")]
        public async Task<IActionResult> CreaRistorante([FromForm] string nome, [FromForm] string indirizzo, [FromForm] string telefono,
                                                   [FromForm] string email, [FromForm] int id_Utente, [FromForm] string latitudine,
                                                   [FromForm] string longitudine, [FromForm] string orariApertura,
                                                   [FromForm] IFormFile immagine)
        {
            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(orariApertura))
            {
                return BadRequest("I dati del ristorante sono mancanti.");
            }

            if (!decimal.TryParse(latitudine, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lat) ||
                !decimal.TryParse(longitudine, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal lon))
            {
                return BadRequest("Coordinate non valide.");
            }

            var ristorante = new Ristorante
            {
                Nome = nome,
                Indirizzo = indirizzo,
                Telefono = telefono,
                Email = email,
                ID_Utente = id_Utente,
                Latitudine = lat,
                Longitudine = lon,
                OrariApertura = JsonConvert.DeserializeObject<List<OrarioApertura>>(orariApertura)
            };

            try
            {
                await _ristoranteService.CreaRistorante(ristorante, immagine);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }

            return Ok(ristorante);
        }

        [HttpGet("GetRestaurantsByUser/{iD_Utente}")]
        public IActionResult GetRestaurantsByUser(int iD_Utente)
        {
            var restaurants = _ristoranteService.GetRestaurantsByUserId(iD_Utente);
            if (restaurants == null || !restaurants.Any())
            {
                return NotFound("Nessun ristorante trovato per l'utente.");
            }
            return Ok(restaurants);
        }
    }
}



