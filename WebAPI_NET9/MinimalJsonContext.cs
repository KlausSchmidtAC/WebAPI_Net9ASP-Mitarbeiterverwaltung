using System.Text.Json.Serialization;
using Domain;

[JsonSerializable(typeof(List<Domain.Mitarbeiter>))]
public partial class MinimalJsonContext : JsonSerializerContext
{
}