using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Domain; 

// TODO: Fertig machen!! Claims, Rollen, etc. noch nicht vertsanden und auch nicht Namerspace!! 


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string TokenSecret = "HaHaHaThisIsASecretKeyHaHaHaHaHaHaThisIsASecretKeyHaHaHa"; // Fügen Sie hier Ihren geheimen Schlüssel hinzu
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(0.25);


   
    [HttpGet("public")]
    public IActionResult PublicEndpoint()
    {
        return Ok("This is a public endpoint accessible without authentication.");
    }

    [HttpGet("protected")]
    [Authorize]
    public IActionResult ProtectedEndpoint()
    {
        return Ok("This is a protected endpoint accessible only with valid authentication.");
    }

    [HttpPost("token")]
    public IActionResult CreateToken([FromBody]TokenGenerationRequest request) // IST MIR NOCH UNVERSTAENDLICH!! 
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TokenSecret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Email),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new("userId", request.UserId.ToString())
        };

        // ✅ LÖSUNG: JsonElement aus JSON-Deserialisierung verwenden
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
                // Fallback für normale Werte
                claims.Add(new Claim(claimPair.Key, claimPair.Value?.ToString() ?? ""));
            }
        }
    
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = "http://localhost:5100", // Set your issuer here
            Audience = "http://localhost:5100", // Set your audience here
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);
        return Ok(jwt);
    }

    }
