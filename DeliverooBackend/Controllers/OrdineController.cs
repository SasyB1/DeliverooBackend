using DeliverooBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeliverooBackend.Controllers
{
    [ApiController]
    public class OrdiniController : ControllerBase
    {
        private readonly OrdineService _ordineService;

        public OrdiniController(OrdineService ordineService)
        {
            _ordineService = ordineService;
        }

        [HttpPost("crea-ordine")]
        public async Task<IActionResult> CreaOrdine([FromForm] int idUtente, [FromForm] int idRistorante)
        {
            if (idUtente <= 0 || idRistorante <= 0)
            {
                return BadRequest("ID utente o ID ristorante non valido.");
            }

            try
            {
                int nuovoOrdineId = await _ordineService.CreaOrdine(idUtente, idRistorante);
                return Ok(new { idOrdine = nuovoOrdineId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpPost("aggiungi-piatto-ordine")]
        public async Task<IActionResult> AggiungiPiattoAOrdine([FromForm] int idOrdine, [FromForm] int idPiatto, [FromForm] int quantita)
        {
            if (idOrdine <= 0 || idPiatto <= 0 || quantita <= 0)
            {
                return BadRequest("Dati non validi.");
            }

            try
            {
                await _ordineService.AggiungiPiattoAOrdine(idOrdine, idPiatto, quantita);
                return Ok(new { message = "Piatto aggiunto all'ordine." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpPost("aggiungi-ingrediente-dettaglio-ordine")]
        public async Task<IActionResult> AggiungiIngredienteADettaglioOrdine([FromForm] int idDettaglioOrdine, [FromForm] int idIngrediente, [FromForm] int quantitaIngrediente)
        {
            if (idDettaglioOrdine <= 0 || idIngrediente <= 0 || quantitaIngrediente <= 0 )
            {
                return BadRequest("Dati non validi.");
            }

            try
            {
                await _ordineService.AggiungiIngredienteADettaglioOrdine(idDettaglioOrdine, idIngrediente, quantitaIngrediente);
                return Ok(new { message = "Ingrediente aggiunto al piatto dell'ordine." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpGet("get-ordini-utente/{idUtente}")]
        public IActionResult GetOrdiniByUtente(int idUtente)
        {
            var ordini = _ordineService.GetOrdiniByUtente(idUtente);
            if (ordini == null || !ordini.Any())
            {
                return NotFound("Nessun ordine trovato per l'utente.");
            }
            return Ok(ordini);
        }
    }
}
