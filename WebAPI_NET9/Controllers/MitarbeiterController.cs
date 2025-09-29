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

        public MitarbeiterController(IMitarbeiterService mitarbeiterService)
        {
            _mitarbeiterService = mitarbeiterService ?? throw new ArgumentNullException(nameof(mitarbeiterService));
        }

        [HttpGet]
        public ActionResult<IEnumerable<Mitarbeiter>> GetAll()
        {
            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            if (mitarbeiterListe.Count == 0)
                return NotFound("Keine Mitarbeiter in der Liste.");
            else
            {
                foreach (var mitarbeiter in mitarbeiterListe)
                {
                    Console.WriteLine(mitarbeiter.ToString());
                }
                return Ok("Inhalt der gesamten MitarbeiterListe: {" + string.Join(" ;", mitarbeiterListe) + " }");
            }
        }


        [HttpGet("search")]
        public ActionResult<IEnumerable<Mitarbeiter>> GetSorted([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return NotFound("Bitte einen gültigen Mitarbeiterfilter eingegeben.");
            }

            var mitarbeiterListe = _mitarbeiterService.SearchMitarbeiter(search).ToList();

            if (search == "isActive")
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

            else if (mitarbeiterListe == null)
            {
                return BadRequest("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
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

            else
            {
                return NotFound("Ungültiger Suchfilter. Bitte 'isActive' oder 'LastName' verwenden.");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<Mitarbeiter> GetMitarbeiter([FromRoute] int id)
        {
            var mitarbeiter = _mitarbeiterService.GetMitarbeiterById(id);

            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }
            else if (mitarbeiter == default || mitarbeiter == null)
            {
                return NotFound($"Mitarbeiter mit der ID = {id} nicht existent.");
            }
            return Ok("Mitarbeiter mit der ID " + id + " gefunden: " + mitarbeiter);
        }

        [HttpGet("birthDate")]
        public ActionResult<IEnumerable<Mitarbeiter>> GetByDate([FromQuery] string? birthDate)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
            {
                return NotFound("Bitte ein Geburtsdatum im Format 'yyyy-MM-dd' eingeben.");
            }

            var aeltereMitarbeiter = _mitarbeiterService.SearchMitarbeiter(birthDate).ToList();

            if (aeltereMitarbeiter == null)
            {
                return BadRequest("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
            }

            if (aeltereMitarbeiter.Count == 0)
            {
                return NotFound($"Kein Mitarbeiter mit früherem Geburtsdatum als {birthDate} gefunden.");
            }

            string result = string.Join(", ", aeltereMitarbeiter.Select(m => m.ToString()));
            return Ok($"alle älteren Mitarbeiter ab {birthDate}: {result}");
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

            var success = _mitarbeiterService.DeleteMitarbeiter(id);

            if (!success)
            {
                return NotFound($"Mitarbeiter mit der ID {id} nicht gefunden.");
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
