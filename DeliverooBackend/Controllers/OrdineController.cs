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
            if (idDettaglioOrdine <= 0 || idIngrediente <= 0 || quantitaIngrediente <= 0)
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

        [HttpGet("get-ingredienti")]
        public async Task<IActionResult> GetAllIngredienti()
        {
            try
            {
                var ingredienti = await _ordineService.GetAllIngredienti();
                return Ok(ingredienti);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpPost("aggiungi-recensione")]
        public async Task<IActionResult> AggiungiRecensione([FromForm] int idOrdine, [FromForm] int idRistorante, [FromForm] int idUtente, [FromForm] int valutazione, [FromForm] string commento)
        {
            if (idOrdine <= 0 || idRistorante <= 0 || idUtente <= 0 || valutazione < 1 || valutazione > 5)
            {
                return BadRequest("Dati non validi.");
            }

            try
            {
                await _ordineService.AggiungiRecensione(idOrdine, idRistorante, idUtente, valutazione, commento);
                return Ok(new { message = "Recensione aggiunta con successo." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
        [HttpGet("recensioni/{idRistorante}")]
        public async Task<IActionResult> GetRecensioniByRistorante(int idRistorante)
        {
            if (idRistorante <= 0)
            {
                return BadRequest("ID ristorante non valido.");
            }

            try
            {
                var recensioni = await _ordineService.GetRecensioniByRistorante(idRistorante);
                if (recensioni == null || !recensioni.Any())
                {
                    return NotFound("Nessuna recensione trovata per questo ristorante.");
                }
                return Ok(recensioni);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

        [HttpGet("get-ordini-ristorante/{idRistorante}")]
        public async Task<IActionResult> GetOrdiniByRistorante(int idRistorante)
        {
            if (idRistorante <= 0)
            {
                return BadRequest("ID ristorante non valido.");
            }

            try
            {
                var ordini = await _ordineService.GetOrdiniByRistorante(idRistorante);
                if (ordini == null || !ordini.Any())
                {
                    return NotFound("Nessun ordine trovato per questo ristorante.");
                }

                return Ok(ordini);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }


        [HttpPut("cambia-stato-ordine")]
        public async Task<IActionResult> CambiaStatoOrdine([FromForm] int idOrdine, [FromForm] string nuovoStato)
        {
            if (idOrdine <= 0 || string.IsNullOrEmpty(nuovoStato))
            {
                return BadRequest("Dati non validi.");
            }

            try
            {
                await _ordineService.CambiaStatoOrdine(idOrdine, nuovoStato);
                return Ok(new { message = "Stato dell'ordine aggiornato con successo." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }

    }
}
