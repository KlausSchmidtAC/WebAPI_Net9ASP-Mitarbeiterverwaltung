using System.Text.Json.Serialization;
using Domain;

[JsonSerializable(typeof(List<Domain.Employee>))]
public partial class MinimalJsonContext : JsonSerializerContext
{
}