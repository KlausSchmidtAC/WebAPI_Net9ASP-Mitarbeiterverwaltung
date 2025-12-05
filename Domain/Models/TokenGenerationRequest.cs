namespace Domain; 
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(TokenGenerationRequest))]
public record TokenGenerationRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public Dictionary<string, object> CustomClaims { get; init; } = new(); // Claims: JSON-Key-Value-Paare
}