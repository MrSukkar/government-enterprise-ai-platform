using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Platform.Integrations.ApiCatalog;

public sealed record OpenApi31ValidationReport(
    string DocumentSha256Digest,
    string SpecificationVersion,
    ImmutableArray<string> OperationIds,
    ImmutableArray<string> Errors)
{
    public OpenApi31ValidationReport RequireAccepted()
    {
        if (!string.Equals(SpecificationVersion, "3.1.0", StringComparison.Ordinal) ||
            Errors.Length != 0 || OperationIds.IsDefaultOrEmpty ||
            OperationIds.Distinct(StringComparer.Ordinal).Count() != OperationIds.Length)
        {
            throw new InvalidOperationException("OpenAPI document failed strict 3.1 validation.");
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(DocumentSha256Digest);
        return this;
    }
}

public interface IOpenApi31ContractValidator
{
    OpenApi31ValidationReport Validate(string document);
}

public sealed class StrictOpenApi31ContractValidator : IOpenApi31ContractValidator
{
    private static readonly ImmutableHashSet<string> OperationMethods =
        ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "get", "put", "post", "delete", "options", "head", "patch", "trace");

    public OpenApi31ValidationReport Validate(string document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(document);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(document)));
        var errors = ImmutableArray.CreateBuilder<string>();
        var operations = ImmutableArray.CreateBuilder<string>();
        string version = string.Empty;

        try
        {
            using var parsed = JsonDocument.Parse(document);
            var root = parsed.RootElement;
            if (!root.TryGetProperty("openapi", out var versionNode) || versionNode.ValueKind != JsonValueKind.String)
                errors.Add("Missing OpenAPI version.");
            else
                version = versionNode.GetString() ?? string.Empty;

            if (!string.Equals(version, "3.1.0", StringComparison.Ordinal)) errors.Add("OpenAPI version must be exactly 3.1.0.");
            if (!root.TryGetProperty("info", out var info) || info.ValueKind != JsonValueKind.Object ||
                !HasText(info, "title") || !HasText(info, "version")) errors.Add("Info title and version are required.");
            if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object || !paths.EnumerateObject().Any())
            {
                errors.Add("At least one path is required.");
            }
            else
            {
                foreach (var path in paths.EnumerateObject())
                {
                    if (!path.Name.StartsWith("/", StringComparison.Ordinal) || path.Value.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add($"Invalid path item '{path.Name}'.");
                        continue;
                    }
                    foreach (var method in path.Value.EnumerateObject().Where(item => OperationMethods.Contains(item.Name)))
                    {
                        if (method.Value.ValueKind != JsonValueKind.Object || !HasText(method.Value, "operationId"))
                            errors.Add($"Operation {method.Name.ToUpperInvariant()} {path.Name} requires operationId.");
                        else
                            operations.Add(method.Value.GetProperty("operationId").GetString()!);
                    }
                }
            }
            if (operations.Count != operations.Distinct(StringComparer.Ordinal).Count()) errors.Add("operationId values must be unique.");
        }
        catch (JsonException exception)
        {
            errors.Add($"Invalid JSON: {exception.Message}");
        }

        return new OpenApi31ValidationReport(digest, version, operations.ToImmutable(), errors.ToImmutable());
    }

    private static bool HasText(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());
}
