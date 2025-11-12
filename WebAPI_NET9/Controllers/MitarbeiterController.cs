using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain;
using Application;
using Domain.Constants;


namespace WebAPI_NET9.Controllers
{   
    [ApiController]
    [Route("api/Mitarbeiter")]
    public class MitarbeiterController : ControllerBase
    {

        IMitarbeiterService _mitarbeiterService;
        ILogger<MitarbeiterController> _logger; // optional: für Logging
        public MitarbeiterController(IMitarbeiterService mitarbeiterService, ILogger<MitarbeiterController> logger)
        {
            _mitarbeiterService = mitarbeiterService ?? throw new ArgumentNullException(nameof(mitarbeiterService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MitarbeiterController initialized.");
        }

    
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetAll()
        {
            _logger.LogInformation("GetAll aufgerufen");

            var operationResult = await _mitarbeiterService.GetAllMitarbeiter();
            if (!operationResult.Success)
            {
                _logger.LogWarning("Keine Mitarbeiter in der Liste.");
                return NotFound(operationResult.ErrorMessage);
            }
            else
            {
                _logger.LogInformation(String.Format("{0} Mitarbeiter gefunden", operationResult.Data.Count()));
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
                _logger.LogInformation("Sortierte Mitarbeiter:" + String.Join(", ", operationResult.Data.ToList()));
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

            _logger.LogInformation("Mitarbeiter mit ID {0} gefunden: {1}", id, operationResult.Data);
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
            _logger.LogInformation("Mitarbeiter mit aelterem Geburtsdatum gefunden: {0}", String.Join(", ", operationResult.Data.ToList()));
            return Content(String.Join(", ", operationResult.Data.ToList()));
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPost]
        public async Task<IActionResult> CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
        {
            var operationResult = await _mitarbeiterService.CreateMitarbeiter(mitarbeiter);

            if (!operationResult.Success)
            {
                return BadRequest(operationResult.ErrorMessage);
            }
            _logger.LogInformation("Neuer Mitarbeiter erstellt: {0}", mitarbeiter.ToString());
            return CreatedAtAction(nameof(CreateMitarbeiter), new { id = mitarbeiter.id }, mitarbeiter);
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
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
            _logger.LogInformation("Mitarbeiter mit der ID {0} wurde deaktiviert bzw. gelöscht.", id);
            return Ok("Mitarbeiter mit der ID " + id + " wurde deaktiviert bzw. gelöscht.");
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
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
            _logger.LogInformation("Mitarbeiter mit der ID {0} wurde aktualisiert: {1}.", id, mitarbeiter.ToString());
            return Ok("Mitarbeiter mit der ID " + id + " wurde aktualisiert.");
        }
    }
}
