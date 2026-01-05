using System.Text.Json.Serialization;
using Domain;

namespace WebAPI_NET9;

[JsonSerializable(typeof(List<Domain.Employee>))]
[JsonSerializable(typeof(IEnumerable<Domain.Employee>))]
[JsonSerializable(typeof(Domain.Employee))]
[JsonSerializable(typeof(Domain.Employee[]))]
[JsonSerializable(typeof(OperationResult))]
[JsonSerializable(typeof(TokenGenerationRequest))]

 public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
