using DeliverooBackend.Models;
using DeliverooBackend.Services;
using Microsoft.AspNetCore.Mvc;
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
    }
}
