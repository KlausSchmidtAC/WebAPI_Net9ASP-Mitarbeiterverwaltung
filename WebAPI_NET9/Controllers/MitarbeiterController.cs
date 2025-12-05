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
        ILogger<MitarbeiterController> _logger; 
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
                _logger.LogWarning("GetAll-Request: Keine Mitarbeiter in der Liste.");
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            else
            {
                _logger.LogInformation("{MitarbeiterCount} Mitarbeiter gefunden", operationResult.Data.Count());
                return Ok(new { Message = "Alle Mitarbeiter", Data = operationResult.Data });
            }
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetSorted([FromQuery] string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {   
                _logger.LogWarning("GetSorted-Request: Ungültiger Mitarbeiterfilter.");
                return NotFound(new { Message = "Bitte einen gültigen Mitarbeiterfilter eingegeben." });
            }
            var operationResult = await _mitarbeiterService.SearchMitarbeiter(search);

            if (!operationResult.Success)
            {   
                _logger.LogError("GetSorted-Request: Fehler bei der Mitarbeitersuche. {0}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }

            else if (search == "isActive")
            {
                if (operationResult.Data.Count() == 0)
                {
                    _logger.LogWarning("GetSorted-Request: Keine aktiven Mitarbeiter in der Liste.");
                    return NotFound(new { Message = "Keine aktiven Mitarbeiter in der Liste." });
                }   
                else
                    return Ok(new {Message = "Alle aktiven Mitarbeiter", Filter = "isActive", Count = operationResult.Data.Count(), Data = operationResult.Data });
            }

            else if (search == "LastName")
            {
                if (operationResult.Data.Count() == 0)
                {
                    _logger.LogWarning("GetSorted-Request: Keine Mitarbeiter mit Nachnamen in der Liste.");
                    return NotFound(new { Message = "Keine Mitarbeiter mit Nachnamen in der Liste." });
                }   
                else
                    return Ok(new {Message = "Alle Mitarbeiter nach Nachname aufsteigend alphabetisch sortiert", Filter = "LastName", Count = operationResult.Data.Count(), Data = operationResult.Data });
            }
            else if (operationResult.Data.Count() == 0)
            {   
                _logger.LogWarning("GetSorted-Request: Kein Mitarbeiter mit früherem Geburtsdatum als {0} gefunden.", search);
                return NotFound(new { Message = $"Kein Mitarbeiter mit früherem Geburtsdatum als {search} gefunden." });
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Sortierte Mitarbeiter: {MitarbeiterList}",
                        string.Join(", ", operationResult.Data.ToList()));
                }
                else
                {
                    _logger.LogInformation("{MitarbeiterCount} Mitarbeiter gefunden", operationResult.Data.Count());
                }
                
                return Ok(new {Message = $"Alle älteren Mitarbeiter ab {search}", Filter = "Älter als " + search, Count = operationResult.Data.Count(), Data = operationResult.Data });
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<Mitarbeiter>> GetMitarbeiter([FromRoute] int id)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("GetMitarbeiter-Request: Unzulässige ID {0} übergeben.", id);
                return BadRequest(new { Message = "Unzulässige ID" });
            }

            var operationResult = await _mitarbeiterService.GetMitarbeiterById(id);

            if (!operationResult.Success)
            {   
                _logger.LogError("GetMitarbeiter-Request: Fehler beim Abrufen des Mitarbeiters. {0}", operationResult.ErrorMessage);
                return NotFound(new { Message = $"Mitarbeiter mit der ID = {id} nicht existent." });
            }

            _logger.LogInformation("Mitarbeiter mit ID {0} gefunden: {1}", id, operationResult.Data);
            return Ok(new {Message = $"Mitarbeiter mit ID {id} gefunden", Filter = "ID", Count = 1, Data = operationResult.Data });
        }

        [Authorize]
        [HttpGet("birthDate")]
        public async Task<ActionResult<IEnumerable<Mitarbeiter>>> GetByDate([FromQuery] string? birthDate)
        {
            if (string.IsNullOrWhiteSpace(birthDate))
            {   
                _logger.LogWarning("GetByDate-Request: Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'.");
                return BadRequest(new { Message = "Ungültiges Datumsformat bzw. Eingabe eines Datums. Bitte verwenden Sie 'yyyy-MM-dd'." });
            }

            var operationResult = await _mitarbeiterService.SearchMitarbeiter(birthDate);

            if (!operationResult.Success)
            {
                _logger.LogError("GetByDate-Request: Fehler bei der Mitarbeitersuche nach Geburtsdatum. {0}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            
            if(_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Mitarbeiter mit aelterem Geburtsdatum als {0} gefunden: {1}", birthDate, operationResult.Data.ToList());
            }
            else
            {
            _logger.LogInformation("Mitarbeiter mit aelterem Geburtsdatum gefunden: {0}", operationResult.Data.Count());
            }
            return Ok(new {Message = $"Mitarbeiter mit aelterem Geburtsdatum als {birthDate} gefunden", Filter = "Älter als " + birthDate, Count = operationResult.Data.Count(), Data = operationResult.Data });
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)] //Keine Policy, nur Claim-Attribut
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPost]
        public async Task<IActionResult> CreateMitarbeiter([FromBody] Mitarbeiter mitarbeiter)
        {
            var operationResult = await _mitarbeiterService.CreateMitarbeiter(mitarbeiter);

            if (!operationResult.Success)
            {
                _logger.LogError("CreateMitarbeiter-Request: Fehler beim Erstellen des Mitarbeiters. {0}", operationResult.ErrorMessage);
                return BadRequest(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("Neuer Mitarbeiter erstellt: {0}", mitarbeiter.ToString());
            return CreatedAtAction(nameof(GetMitarbeiter), new { id = mitarbeiter.id }, new {Message = "Neuer Mitarbeiter erstellt", Data = mitarbeiter});
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMitarbeiter([FromRoute] int id)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("DeleteMitarbeiter-Request: Ungültige ID {0}", id);
                return BadRequest(new { Message = "Unzulässige ID" });
            }

            var operationResult = await _mitarbeiterService.DeleteMitarbeiter(id);

            if (!operationResult.Success)
            {   
                _logger.LogError("DeleteMitarbeiter-Request: Fehler beim Löschen des Mitarbeiters. {0}", operationResult.ErrorMessage);
                return NotFound(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("Mitarbeiter mit der ID {0} wurde deaktiviert bzw. gelöscht.", id);
            return NoContent();
        }

        // [Authorize(Policy = Domain.Constants.IdentityData.Policies.AdminOnly)]
        [Authorize]
        [RequiresClaim(IdentityData.Claims.AdminRole, "true")]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateMitarbeiter([FromRoute] int id, [FromBody] Mitarbeiter mitarbeiter)
        {
            if (id <= 0)
            {   
                _logger.LogWarning("UpdateMitarbeiter-Request: Unzulässige ID {0}", id);
                return BadRequest(new { Message = "Unzulässige ID" });
            }

            var operationResult = await _mitarbeiterService.UpdateMitarbeiter(id, mitarbeiter);

            if (!operationResult.Success)
            {   
                _logger.LogError("UpdateMitarbeiter-Request: Fehler beim Aktualisieren des Mitarbeiters. {0}", operationResult.ErrorMessage);
                return BadRequest(new { Message = operationResult.ErrorMessage });
            }
            _logger.LogInformation("Mitarbeiter mit der ID {0} wurde aktualisiert: {1}.", id, mitarbeiter.ToString());
            return Ok(new {Message = $"Mitarbeiter mit der ID {id} wurde erfolgreich aktualisiert", Data = mitarbeiter} );
        }
    }
}
