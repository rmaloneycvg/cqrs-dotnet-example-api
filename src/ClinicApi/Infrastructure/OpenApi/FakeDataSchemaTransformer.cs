using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClinicApi.Infrastructure.OpenApi;

/// <summary>
/// OpenAPI schema transformer that populates default values with realistic fake data.
/// Makes Scalar's "Try It" UI pre-fill request bodies with sensible defaults.
/// Users can always override these values before sending.
///
/// Only applied in Development — production does not expose OpenAPI.
/// Uses schema.Default (writable) since schema.Example is read-only on IOpenApiSchema.
/// </summary>
public class FakeDataSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken ct)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
            return Task.CompletedTask;

        foreach (var (name, prop) in schema.Properties)
        {
            if (prop is not OpenApiSchema concreteProp) continue;
            if (concreteProp.Default is not null) continue;

            var example = GenerateExample(name.ToLowerInvariant());
            if (example is not null)
                concreteProp.Default = example;
        }

        return Task.CompletedTask;
    }

    private static JsonNode? GenerateExample(string name) => name switch
    {
        // Person
        "firstname" or "first_name" => JsonValue.Create("Sarah"),
        "lastname" or "last_name" => JsonValue.Create("Mitchell"),
        "fullname" => JsonValue.Create("Sarah Mitchell"),
        "email" => JsonValue.Create($"sarah.mitchell.{Random.Shared.Next(100, 999)}@example.com"),
        "phonenumber" or "phone" => JsonValue.Create($"{Random.Shared.Next(200, 999)}-{Random.Shared.Next(100, 999)}-{Random.Shared.Next(1000, 9999)}"),
        "providername" or "prescribername" => JsonValue.Create("Dr. Rodriguez"),
        "patientname" => JsonValue.Create("Sarah Mitchell"),

        // Address
        "street" => JsonValue.Create($"{Random.Shared.Next(100, 9999)} Oak Street"),
        "city" => JsonValue.Create("Denver"),
        "state" => JsonValue.Create("CO"),
        "zipcode" or "zip" => JsonValue.Create("80202"),

        // Dates
        "dateofbirth" => JsonValue.Create(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-Random.Shared.Next(25, 70))).ToString("yyyy-MM-dd")),
        "scheduledat" => JsonValue.Create(
            DateTime.UtcNow.AddDays(Random.Shared.Next(1, 30)).ToString("yyyy-MM-ddTHH:mm:ssZ")),
        "prescribeddate" or "effectivedate" => JsonValue.Create(
            DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")),
        "expirationdate" => JsonValue.Create(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd")),
        "duedate" => JsonValue.Create(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd")),
        "olderthan" => JsonValue.Create(
            DateTime.UtcNow.AddYears(-2).ToString("yyyy-MM-ddTHH:mm:ssZ")),
        "createdat" or "updatedat" or "issuedat" or "processedat" => JsonValue.Create(
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),

        // IDs
        "externalid" or "external_id" => JsonValue.Create($"EXT-{Random.Shared.Next(10000, 99999)}"),
        "patientid" => JsonValue.Create(Guid.NewGuid().ToString()),
        "invoiceid" => JsonValue.Create(Guid.NewGuid().ToString()),
        "prescriptionid" => JsonValue.Create(Guid.NewGuid().ToString()),
        "id" => JsonValue.Create(Guid.NewGuid().ToString()),
        "providerid" => JsonValue.Create($"PROV-{Random.Shared.Next(1000, 9999)}"),
        "transactionid" => JsonValue.Create($"TXN-{Random.Shared.Next(100000, 999999)}"),

        // Financial
        "amount" or "monthlyamount" or "totaloutstanding" or "outstandingbalance" =>
            JsonValue.Create(Math.Round(Random.Shared.NextDouble() * 450 + 50, 2)),
        "paidamount" => JsonValue.Create(0.00),
        "minimumbalance" or "minbalance" => JsonValue.Create(0.01),
        "invoicenumber" => JsonValue.Create($"INV-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(1000, 9999)}"),

        // Medical
        "medicationname" => JsonValue.Create(
            (new[] { "Amoxicillin", "Lisinopril", "Metformin", "Atorvastatin", "Omeprazole" })[Random.Shared.Next(5)]),
        "dosage" => JsonValue.Create(
            (new[] { "250mg", "500mg", "10mg", "20mg", "40mg" })[Random.Shared.Next(5)]),
        "frequency" => JsonValue.Create(
            (new[] { "Once daily", "Twice daily", "Every 8 hours", "As needed" })[Random.Shared.Next(4)]),
        "refillsremaining" or "totalrefills" => JsonValue.Create(Random.Shared.Next(1, 6)),
        "method" => JsonValue.Create("CreditCard"),

        // Text
        "description" or "reason" => JsonValue.Create("Routine follow-up appointment"),
        "notes" => JsonValue.Create("Patient reported improvement in symptoms. Continue current treatment plan."),

        // Insurance
        "policynumber" => JsonValue.Create($"POL-{Random.Shared.Next(10000000, 99999999)}"),
        "groupnumber" => JsonValue.Create($"GRP-{Random.Shared.Next(100000, 999999)}"),

        // Pagination & counts
        "page" => JsonValue.Create(1),
        "pagesize" => JsonValue.Create(20),
        "totalcount" or "createdcount" or "updatedcount" or "affectedcount"
            or "deletedcount" or "invoicecount" or "successcount" => JsonValue.Create(10),
        "failurecount" or "errorcount" => JsonValue.Create(0),
        "totalpages" => JsonValue.Create(1),

        // Batch
        "batchsize" => JsonValue.Create(500),
        "inactivedaysthreshold" => JsonValue.Create(730),

        // Duration
        "duration" => JsonValue.Create("00:30:00"),

        // Status
        "status" => JsonValue.Create("Active"),

        // Error/validation
        "type" => JsonValue.Create("https://tools.ietf.org/html/rfc9110#section-15.5.1"),
        "title" => JsonValue.Create("Validation Failed"),
        "detail" => JsonValue.Create("One or more validation errors occurred."),
        "instance" => JsonValue.Create("/api/patients"),

        _ => null
    };
}
