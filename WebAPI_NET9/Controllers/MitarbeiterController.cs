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
            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();

            if (mitarbeiterListe.Count == 0)
            {
                return NotFound("Keine Mitarbeiter in der Liste zum Sortieren.");
            }

            if (string.IsNullOrWhiteSpace(search))
            {
                return NotFound($"Bitte einen Mitarbeiterfilter eingeben'{search}'.");

            }
            else if (search == "isActive")
            {
                var aktiveMitarbeiter = mitarbeiterListe.FindAll(m => m.IsActive == true);
                if (aktiveMitarbeiter.Count == 0)
                {
                    return NotFound("Kein aktiver Mitarbeiter gefunden.");
                }
                return Ok("Alle aktiven Mitarbeiter: {" + string.Join("; ", aktiveMitarbeiter) + "}");
            }

            else if (search == "LastName")
            {
                var sortierteMitarbeiter = mitarbeiterListe.OrderBy(m => m.LastName).Reverse().ToList();
                return Ok("Alle Mitarbeiter nach Nachname aufsteigend alphabetisch sortiert: {" + string.Join("; ", sortierteMitarbeiter) + "}");
            }
            else
                return NotFound("Keinen solchen Mitarbeiterfilter gefunden.");
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
            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            if (DateOnly.TryParseExact(birthDate, "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateOnly date))
            {
                var birthDate_parsed = date;
                var aeltere = mitarbeiterListe.FindAll((x) => DateOnly.Parse(x.BirthDate) < birthDate_parsed);
                if (aeltere == null || aeltere.Count == 0)
                    return NotFound($"Kein Mitarbeiter mit früherem Geburtsdatum als {birthDate} gefunden.");
                string result = string.Join(", ", aeltere.Select(m => m.ToString()));
                return Ok($"alle älteren Mitarbeiter ab {birthDate_parsed}: {result}");
            }
            else
            {
                return BadRequest("Eingabe ist kein Geburtsdatum oder hat das falsche Format. Bitte verwenden Sie das Format 'yyyy-MM-dd'");
            }
        }

        [HttpPost]
        public IActionResult CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
        {

            DateOnly date;
            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            try
            {
                if (mitarbeiter == null)
                {
                    return BadRequest("Mitarbeiterdaten sind korrumpiert oder leer.");
                }
                else if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
                {
                    return BadRequest("Ein Vorname und ein Nachname sind erforderlich.");
                }
                else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
                {
                    return BadRequest("Ein Geburtsdatum im Format 'yyyy-MM-dd' ist erforderlich.");
                }
                else if (DateOnly.TryParseExact(
                    mitarbeiter.BirthDate,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateParsed
                ) == false)
                {
                    return BadRequest($"Eingegebenes Geburtsdatum {mitarbeiter.BirthDate} hat ein ungültiges Format. Bitte verwenden Sie das Format 'yyyy-MM-dd'.");
                }
                else if (mitarbeiterListe.Any(m => m.FirstName == mitarbeiter.FirstName && m.LastName == mitarbeiter.LastName && m.BirthDate == mitarbeiter.BirthDate))
                {
                    return BadRequest("Ein Mitarbeiter mit dem gleichen Vornamen, Nachnamen und Geburtsdatum existiert bereits.");
                }
                else
                {
                    date = dateParsed;
                }
            }
            catch (FormatException ex)
            {
                return BadRequest($"Fehler beim Verarbeiten des Geburtsdatums: invalide Zeichen eingegeben! // {ex.Message}");
            }

            int newID = mitarbeiterListe.Max(m => m.id) + 1;
            Mitarbeiter newOne = new(newID, mitarbeiter.FirstName, mitarbeiter.LastName, date.ToString("yyyy-MM-dd"), true);
            _mitarbeiterService.CreateMitarbeiter(newOne);
            return CreatedAtAction(nameof(CreateMitarbeiter), new { id = newID }, newOne);
            // return Ok(newOne);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteMitarbeiter([FromRoute] int id)
        {

            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            var mitarbeiter = mitarbeiterListe.Find(m => m.id == id); // Find (...) gibt Objekt-reference zurück , keine Kopie des Mitarbeiter-Objektes
            if (mitarbeiter == default)
                return NotFound($"Mitarbeiter mit der ID {id} nicht gefunden.");
            _mitarbeiterService.DeleteMitarbeiter(id);
            return Content("Mitarbeiter mit der ID " + id + " wurde deaktiviert bzw. gelöscht.");
        }

        [HttpPatch("{id}")]
        public IActionResult UpdateMitarbeiter([FromRoute] int id, [FromBody] Mitarbeiter mitarbeiter)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }
            else if (mitarbeiter == null)
            {
                return BadRequest("Mitarbeiterdaten sind korrumpiert oder leer.");
            }

            var mitarbeiterListe = _mitarbeiterService.GetAllMitarbeiter().ToList();
            var existingMitarbeiter = mitarbeiterListe.Find(m => m.id == id);

            if (existingMitarbeiter == default || existingMitarbeiter == null)
                return NotFound($"Mitarbeiter mit der ID {id} nicht gefunden bzw. nicht existent.");

            else if (string.IsNullOrWhiteSpace(mitarbeiter.FirstName) || string.IsNullOrWhiteSpace(mitarbeiter.LastName))
            {
                return BadRequest("Ein neuer Vorname oder Nachname sind erforderlich.");
            }
            else if (string.IsNullOrWhiteSpace(mitarbeiter.BirthDate.ToString()))
            {
                return BadRequest("Ein neues Geburtsdatum ist erforderlich.");
            }
            else if (DateOnly.TryParseExact(
                    mitarbeiter.BirthDate.ToString(),
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateOnly dateExact
                ) == false)
            {
                return BadRequest("Format oder Zeichen im Geburtsdatum ist ungültig. Bitte verwenden Sie das Format 'yyyy-MM-dd'.");
            }
            else if (mitarbeiter.FirstName == existingMitarbeiter.FirstName && mitarbeiter.LastName == existingMitarbeiter.LastName && mitarbeiter.BirthDate == existingMitarbeiter.BirthDate && mitarbeiter.IsActive == existingMitarbeiter.IsActive)
            {
                return BadRequest("Es wurden keine geänderten Daten eingegeben.");
            }
            Mitarbeiter patchedMitarbeiter = new(id, mitarbeiter.FirstName, mitarbeiter.LastName, mitarbeiter.BirthDate.ToString(), mitarbeiter.IsActive);
            _mitarbeiterService.UpdateMitarbeiter(patchedMitarbeiter);
            return CreatedAtAction(nameof(UpdateMitarbeiter), new { id = id }, patchedMitarbeiter);
        }
    }
}