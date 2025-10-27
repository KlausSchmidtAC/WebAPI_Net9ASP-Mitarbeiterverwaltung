using Microsoft.AspNetCore.Mvc;
using Domain;
using Application;


namespace WebAPI_NET9.Controllers
{
    [ApiController]
    [Route("api/Mitarbeiter")]
    public class MitarbeiterController : ControllerBase
    {

        IMitarbeiterService _mitarbeiterService;
        // ILogger<MitarbeiterController> _logger; // optional: für Logging
        public MitarbeiterController(IMitarbeiterService mitarbeiterService) // , ILogger<MitarbeiterController> logger)
        {
            _mitarbeiterService = mitarbeiterService ?? throw new ArgumentNullException(nameof(mitarbeiterService));
            // _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // _logger.LogInformation("MitarbeiterController initialized.");
        }

        [HttpGet]
        public ActionResult<IEnumerable<Mitarbeiter>> GetAll()
        {
            // _logger.LogInformation("GetAll aufgerufen");

            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            if (mitarbeiterListe.Count == 0)
            {
                // _logger.LogWarning("Keine Mitarbeiter in der Liste.");
                return NotFound("Keine Mitarbeiter in der Liste.");
            }
            else
            {
                foreach (var mitarbeiter in mitarbeiterListe)
                {
                    Console.WriteLine(mitarbeiter.ToString());
                }
                // _logger.LogInformation($"{mitarbeiterListe.Count} Mitarbeiter gefunden");
                return Ok(mitarbeiterListe);
            }
        }


        [HttpGet("search")]
        public ActionResult<IEnumerable<Mitarbeiter>> GetSorted([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return NotFound("Bitte einen gültigen Mitarbeiterfilter eingegeben.");
            }
            var nullOrList = _mitarbeiterService.SearchMitarbeiter(search);
            var mitarbeiterListe = (nullOrList != null) ? nullOrList.ToList() : null;

            if(mitarbeiterListe == null)
            {
                return NotFound("Ungültiger Suchfilter. Bitte 'isActive' oder 'LastName' oder ein Datum im Format 'yyyy-MM-dd' verwenden.");
            }
            else if (search == "isActive")
            {
                if (mitarbeiterListe.Count == 0)
                    return NotFound("Keine aktiven Mitarbeiter in der Liste.");
                else
                    return Ok("Alle aktiven Mitarbeiter: {" + string.Join("; ", mitarbeiterListe) + "}");
            }

            else if (search == "LastName")
            {
                if (mitarbeiterListe.Count == 0)
                    return NotFound("Keine Mitarbeiter in der Liste.");
                else
                    return Ok("Alle Mitarbeiter nach Nachname aufsteigend alphabetisch sortiert: {" + string.Join("; ", mitarbeiterListe) + "}");
            }
            else if (mitarbeiterListe.Count == 0)
            {
                return NotFound($"Kein Mitarbeiter mit früherem Geburtsdatum als {search} gefunden.");
            }
            else if (DateOnly.TryParseExact(search, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                return Ok($"Alle älteren Mitarbeiter ab {search}: {"{" + string.Join("; ", mitarbeiterListe) + "}"}");
            }
            
            return BadRequest("Ungültiger Suchfilter oder interner Fehler.");
        }

        [HttpGet("{id}")]
        public ActionResult<Mitarbeiter> GetMitarbeiter([FromRoute] int id)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var mitarbeiter = _mitarbeiterService.GetMitarbeiterById(id);
            
            if (mitarbeiter == null)
            {
                return NotFound($"Mitarbeiter mit der ID = {id} nicht existent.");
            }
            
            return Ok(mitarbeiter); // Mitarbeiter-Objekt zurückgeben, nicht String
        }

        [HttpGet("birthDate")]
        public ActionResult<IEnumerable<Mitarbeiter>> GetByDate([FromQuery] string? birthDate)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
            {
                return BadRequest("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
            }

            var listOrNull = _mitarbeiterService.SearchMitarbeiter(birthDate);
            var aeltereMitarbeiter = (listOrNull != null) ? listOrNull.ToList() : null;

            if (aeltereMitarbeiter == null)
            {
                return BadRequest("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
            }

            if (aeltereMitarbeiter.Count == 0)
            {
                return NotFound($"Kein Mitarbeiter mit früherem Geburtsdatum als {birthDate} gefunden.");
            }

            return Ok(aeltereMitarbeiter); // Liste zurückgeben, nicht String
        }

        [HttpPost]
        public IActionResult CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
        {
            string errorMessage = string.Empty;
            var success = _mitarbeiterService.CreateMitarbeiter(mitarbeiter, out errorMessage);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return CreatedAtAction(nameof(CreateMitarbeiter), new { id = mitarbeiter.id }, mitarbeiter);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMitarbeiter([FromRoute] int id)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var success = _mitarbeiterService.DeleteMitarbeiter(id, out string? errorMessage);

            if (!success)
            {
                return NotFound(errorMessage);
            }

            return Content("Mitarbeiter mit der ID " + id + " wurde deaktiviert bzw. gelöscht.");
        }

        [HttpPatch("{id}")]
        public IActionResult UpdateMitarbeiter([FromRoute] int id, [FromBody] Mitarbeiter mitarbeiter)
        {
            string errorMessage = string.Empty;
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var success = _mitarbeiterService.UpdateMitarbeiter(id, mitarbeiter, out errorMessage);

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            return NoContent();
        }
    }
}
