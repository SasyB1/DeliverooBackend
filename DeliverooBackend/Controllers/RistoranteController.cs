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
        [HttpPost("create-menu")]
        public async Task<IActionResult> CreateMenu([FromForm] string nome, [FromForm] int idRistorante)
        {
            if (string.IsNullOrEmpty(nome))
            {
                return BadRequest("Il nome del menu è richiesto.");
            }

            var menu = new Menu
            {
                Nome = nome,
                ID_Ristorante = idRistorante
            };

            try
            {
                await _ristoranteService.CreaMenu(menu);
                return Ok(menu);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
        [HttpPost("create-piatto")]
        public async Task<IActionResult> CreatePiatto([FromForm] string nome, [FromForm] string descrizione, [FromForm] decimal prezzo, [FromForm] int idMenu, [FromForm] IFormFile immagine)
        {
            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(descrizione) || prezzo <= 0)
            {
                return BadRequest("I dati del piatto sono incompleti.");
            }

            var piatto = new Piatto
            {
                Nome = nome,
                Descrizione = descrizione,
                Prezzo = prezzo,
                ID_Menu = idMenu
            };

            try
            {
                await _ristoranteService.CreaPiatto(piatto, immagine);
                return Ok(piatto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }


        [HttpGet("get-menus/{idRistorante}")]
        public IActionResult GetMenus(int idRistorante)
        {
            var menus = _ristoranteService.GetMenusByRestaurantId(idRistorante);
            if (menus == null || !menus.Any())
            {
                return Ok(new List<Menu>());
            }
            return Ok(menus);
        }

        [HttpGet("get-piatti/{idMenu}")]
        public IActionResult GetPiattiByMenu(int idMenu)
        {
            var piatti = _ristoranteService.GetPiattiByMenuId(idMenu)
                                           .Where(p => !p.Cancellato) 
                                           .ToList();
            if (piatti == null || !piatti.Any())
            {
                return NotFound();
            }
            return Ok(piatti);
        }

        [HttpPost("aggiorna-categorie")]
        public async Task<IActionResult> AggiornaCategorieRistorante([FromForm] int idRistorante, [FromForm] string categoryIds)
        {
            if (string.IsNullOrEmpty(categoryIds))
            {
                return BadRequest("Nessuna categoria selezionata.");
            }

            List<int> selectedCategories = JsonConvert.DeserializeObject<List<int>>(categoryIds);

            try
            {
                await _ristoranteService.AggiornaCategorieRistorante(idRistorante, selectedCategories);
                return Ok(new { message = "Categorie aggiornate correttamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Errore interno del server: {ex.Message}" });
            }
        }

        [HttpGet("categorie")]
        public IActionResult GetCategorie()
        {
            var categorie = _ristoranteService.GetCategorie();
            if (categorie == null || !categorie.Any())
            {
                return NotFound("Nessuna categoria trovata.");
            }
            return Ok(categorie);
        }

        [HttpGet("get-categorie-associate/{idRistorante}")]
        public IActionResult GetCategorieAssociate(int idRistorante)
        {
            var categorieAssociate = _ristoranteService.GetCategorieAssociate(idRistorante);

            if (categorieAssociate == null || !categorieAssociate.Any())
            {
                return NotFound("Nessuna categoria associata.");
            }

            return Ok(categorieAssociate);
        }

        [HttpDelete("delete-piatto/{idPiatto}")]
        public async Task<IActionResult> DeletePiatto(int idPiatto)
        {
            try
            {
                bool isDeleted = await _ristoranteService.DeletePiatto(idPiatto);

                if (isDeleted)
                {
                    return Ok(new { message = "Piatto eliminato con successo." });
                }
                else
                {
                    return NotFound(new { message = "Piatto non trovato." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore interno del server.", error = ex.Message });
            }
        }
        [HttpDelete("delete-menu/{idMenu}")]
        public async Task<IActionResult> DeleteMenu(int idMenu)
        {
            try
            {
                bool isDeleted = await _ristoranteService.DeleteMenu(idMenu);

                if (isDeleted)
                {
                    return Ok(new { message = "Menu e piatti associati cancellati con successo." });
                }
                else
                {
                    return NotFound(new { message = "Menu non trovato." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore interno del server.", error = ex.Message });
            }
        }

        [HttpPut("update-menu")]
        public async Task<IActionResult> AggiornaMenu([FromForm] int idMenu, [FromForm] string nuovoNome)
        {
            if (string.IsNullOrEmpty(nuovoNome))
            {
                return BadRequest("Il nome del menu è richiesto.");
            }

            try
            {
                bool success = await _ristoranteService.AggiornaMenu(idMenu, nuovoNome);

                if (success)
                {
                    return Ok(new { message = "Menu aggiornato con successo." });
                }
                else
                {
                    return NotFound(new { message = "Menu non trovato." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Errore interno del server.", error = ex.Message });
            }
        }


    }
}



