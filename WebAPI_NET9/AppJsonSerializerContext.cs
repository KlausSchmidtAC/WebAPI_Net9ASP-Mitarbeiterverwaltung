using System.Text.Json.Serialization;
using Domain;

namespace WebAPI_NET9;

[JsonSerializable(typeof(List<Domain.Mitarbeiter>))]
[JsonSerializable(typeof(IEnumerable<Domain.Mitarbeiter>))]
[JsonSerializable(typeof(Domain.Mitarbeiter))]
[JsonSerializable(typeof(Domain.Mitarbeiter[]))]

 public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
