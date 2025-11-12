using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Domain; 


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string TokenSecret = "HaHaHaThisIsASecretKeyHaHaHaHaHaHaThisIsASecretKeyHaHaHa"; // Fügen Sie hier Ihren geheimen Schlüssel hinzu
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(0.25);

    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        _logger.LogInformation("Öffentlicher Endpunkt aufgerufen.");
        return Ok("This is a public endpoint accessible without authentication.");
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult ProtectedEndpoint()
    {   
        _logger.LogInformation("Geschützter Endpunkt aufgerufen.");
        return Ok("This is a protected endpoint accessible only with valid authentication.");
    }

    [HttpPost("token")]
    public IActionResult CreateToken([FromBody]TokenGenerationRequest request) 
    {
        var jwtConfig = _configuration.GetSection("JWTSettings");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TokenSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new("userId", request.UserId.ToString())
        };

        
        foreach(var claimPair in request.CustomClaims)
        {
            if (claimPair.Value is JsonElement jsonElement)
            {
                var valueType = jsonElement.ValueKind switch
                {
                    JsonValueKind.True => ClaimValueTypes.Boolean,
                    JsonValueKind.False => ClaimValueTypes.Boolean,
                    JsonValueKind.Number => ClaimValueTypes.Double,
                    _ => ClaimValueTypes.String
                };
                claims.Add(new Claim(claimPair.Key, jsonElement.ToString(), valueType));
            }
            else
            {
                claims.Add(new Claim(claimPair.Key, claimPair.Value?.ToString() ?? ""));
            }
        }
    
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = jwtConfig["Issuer"],           
            Audience = jwtConfig["Audience"],       
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);
        
        // Nur Metadaten, keine sensitiven Claim-Werte geloggt
        _logger.LogInformation("JWT-Token erstellt für Benutzer: {Username}, Claims-Anzahl: {ClaimsCount}, Claim-Typen: {ClaimTypes} Gültig bis: {ExpiryTime}", 
            request.Username, 
            claims.Count, 
            string.Join(", ", claims.Select(c => c.Type)),
            tokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            
        return Ok(jwt);
    }

    }
