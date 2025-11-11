using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetAll()
        {
            // _logger.LogInformation("GetAll aufgerufen");

            var operationResult = await _mitarbeiterService.GetAllMitarbeiter();
            if (!operationResult.Success)
            {
                // _logger.LogWarning("Keine Mitarbeiter in der Liste.");
                return NotFound(operationResult.ErrorMessage);
            }
            else
            {
                foreach (var mitarbeiter in operationResult.Data)   //operationResult.Success hat per Design niemals Null oder ein leeres IEnumerable als Data
                {
                    Console.WriteLine(mitarbeiter.ToString());
                }
                // _logger.LogInformation($"{mitarbeiterListe.Count} Mitarbeiter gefunden");
                return Content(String.Join(", ", operationResult.Data.ToList()));
            }
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetSorted([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return NotFound("Bitte einen gültigen Mitarbeiterfilter eingegeben.");
            }
            var operationResult = await _mitarbeiterService.SearchMitarbeiter(search);

            if (!operationResult.Success)
            {
                return NotFound(operationResult.ErrorMessage);
            }

            else if (search == "isActive")
            {
                if (operationResult.Data.Count() == 0)
                    return NotFound("Keine aktiven Mitarbeiter in der Liste.");
                else
                    return Content("Alle aktiven Mitarbeiter: {" + string.Join("; ", operationResult.Data.ToList()) + "}");
            }

            else if (search == "LastName")
            {
                if (operationResult.Data.Count() == 0)
                    return NotFound("Keine Mitarbeiter in der Liste.");
                else
                    return Content("Alle Mitarbeiter nach Nachname aufsteigend alphabetisch sortiert: {" + string.Join("; ", operationResult.Data.ToList()) + "}");
            }
            else if (operationResult.Data.Count() == 0)
            {
                return NotFound($"Kein Mitarbeiter mit früherem Geburtsdatum als {search} gefunden.");
            }
            else
            {
                return Content($"Alle älteren Mitarbeiter ab {search}: {"{" + string.Join("; ", operationResult.Data.ToList()) + "}"}");
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Mitarbeiter>> GetMitarbeiter([FromRoute] int id)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var operationResult = await _mitarbeiterService.GetMitarbeiterById(id);

            if (!operationResult.Success)
            {
                return NotFound($"Mitarbeiter mit der ID = {id} nicht existent.");
            }

            return Content(String.Join(", ", operationResult.Data.ToString()));
        }

        [Authorize]
        [HttpGet("birthDate")]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetByDate([FromQuery] string? birthDate)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
            {
                return BadRequest("Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
            }

            var operationResult = await _mitarbeiterService.SearchMitarbeiter(birthDate);
            
            if (!operationResult.Success)
            {
                return NotFound(operationResult.ErrorMessage);
            }

            return Content(String.Join(", ", operationResult.Data.ToList()));
        }

        [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [HttpPost]
        public async Task<IActionResult> CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
        {
            var operationResult = await _mitarbeiterService.CreateMitarbeiter(mitarbeiter);

            if (!operationResult.Success)
            {
                return BadRequest(operationResult.ErrorMessage);
            }

            return CreatedAtAction(nameof(CreateMitarbeiter), new { id = mitarbeiter.id }, mitarbeiter);
        }

        [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMitarbeiter([FromRoute] int id)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var operationResult = await _mitarbeiterService.DeleteMitarbeiter(id);

            if (!operationResult.Success)
            {
                return NotFound(operationResult.ErrorMessage);
            }

            return Ok("Mitarbeiter mit der ID " + id + " wurde deaktiviert bzw. gelöscht.");
        }

        [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateMitarbeiter([FromRoute] int id, [FromBody] Mitarbeiter mitarbeiter)
        {
            if (id <= 0 || id > int.MaxValue)
            {
                return BadRequest("Unzulässige ID");
            }

            var operationResult = await _mitarbeiterService.UpdateMitarbeiter(id, mitarbeiter);

            if (!operationResult.Success)
            {
                return BadRequest(operationResult.ErrorMessage);
            }

            return Ok("Mitarbeiter mit der ID " + id + " wurde aktualisiert.");
        }
    }
}
