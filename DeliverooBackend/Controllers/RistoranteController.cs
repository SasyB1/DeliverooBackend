using DeliverooBackend.Models;
using DeliverooBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;

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
        public ActionResult<List<Ristorante>> GetRistorantiVicini(double latitudine, double longitudine, double distanzaMassimaKm)
        {
            var ristorantiVicini = _ristoranteService.GetRistorantiVicini(latitudine, longitudine, distanzaMassimaKm);
            return Ok(ristorantiVicini);
        }

        [HttpPost("crea-ristorante")]
        public async Task<IActionResult> CreaRistorante([FromForm] Ristorante ristorante, [FromForm] IFormFile immagine)
        {
            if (ristorante == null)
            {
                return BadRequest("I dati del ristorante sono mancanti.");
            }

            Console.WriteLine($"Nome: {ristorante.Nome}");
            Console.WriteLine($"Orari Apertura: {string.Join(", ", ristorante.OrariApertura.Select(o => $"{o.GiornoSettimana}: {o.OraApertura}-{o.OraChiusura}"))}");

            await _ristoranteService.CreaRistorante(ristorante, immagine);
            return Ok(ristorante);
        }
    }
}



