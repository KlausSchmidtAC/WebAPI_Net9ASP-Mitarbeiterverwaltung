using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Domain; 
using Swashbuckle.AspNetCore.Annotations;


[ApiController]
[Route("api/auth")]
[SwaggerTag("Authentication – JWT Token management")]
public class AuthController : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(0.25);

    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    ///<summary>Public test endpoint</summary>
    ///<remarks> 
    ///This endpoint for testing is accessible without authentication.
    ///</remarks>
    [HttpGet("public")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Public test endpoint", OperationId = "Auth_Public")]
    [SwaggerResponse(200, "Public endpoint accessed successfully")]
    public IActionResult PublicEndpoint(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Public endpoint called.");
        return 
        Ok(new { 
                 Message = "Public endpoint accessed successfully",
                AccessLevel = "Public",
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>Protected Testing-Endpoint</summary>
    /// <remarks>
    /// Requires a valid JWT Bearer token in the Authorization header.
    /// Use the /api/auth/token endpoint to generate a token with custom claims for testing.
    /// </remarks>
    [HttpGet("protected")]
    [Authorize]
    [SwaggerOperation(OperationId = "Auth_Protected")]
    [SwaggerResponse(200, "Protected endpoint accessed successfully")]
    [SwaggerResponse(401, "Unauthorized – valid JWT required")]
    public IActionResult ProtectedEndpoint(CancellationToken cancellationToken = default)
    {   
        _logger.LogInformation("Protected endpoint called.");
        return Ok(new { 
            Message = "Protected endpoint accessed successfully",
            AccessLevel = "Protected",
            Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>Generate JWT-Token</summary>
    /// <remarks>
    /// Creates a JWT token based on the provided user data and custom claims.
    /// Creates a signed HS256 Bearer token. Token validity: 15 minutes.
    /// Example: POST /api/auth/token with JSON body:
    /// {username: "testuser", password: "password123", email: "testuser@user.com", "customClaims": {"isAdmin": true, "department": "IT"}}
    /// </remarks>
    [HttpPost("token")]
    [AllowAnonymous]
    [SwaggerOperation(
    OperationId = "Auth_CreateToken"
    )]
    [SwaggerResponse(200, "Token created successfully")]
    [SwaggerResponse(400, "Invalid request data")]
    public IActionResult CreateToken([FromBody][SwaggerRequestBody("Token request data", Required = true)] TokenGenerationRequest request, CancellationToken cancellationToken = default) 
    {
        var jwtConfig = _configuration.GetSection("JWTSettings");
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(jwtConfig["SecretKey"]);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Email, request.Email),
            // new("userId", request.UserId.ToString())
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
        
        // Only metadata logged, no sensitive claim values
        _logger.LogInformation("JWT token created for user: {Username}, Claims count: {ClaimsCount}, Claim types: {ClaimTypes} Valid until: {ExpiryTime}", 
            request.Username, 
            claims.Count, 
            string.Join(", ", claims.Select(c => c.Type)),
            tokenDescriptor.Expires?.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            
        return Ok(new {
                Message = "Token created successfully",
                Data = new {
                    Token = jwt,
                    TokenType = "Bearer",
                    ExpiresIn = (int)TokenLifetime.TotalSeconds,
                    ExpiresAt = tokenDescriptor.Expires,
                    User = request.Username,
                    ClaimsCount = claims.Count
                }
            });
    }
}
