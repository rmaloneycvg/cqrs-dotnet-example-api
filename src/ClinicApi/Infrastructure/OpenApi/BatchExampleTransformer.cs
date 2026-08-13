using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ClinicApi.Infrastructure.OpenApi;

/// <summary>
/// Adds realistic multi-record example payloads to batch/bulk import endpoints.
/// Scalar's "Try It" will pre-fill with representative data volumes.
/// </summary>
public class BatchExampleTransformer : IOpenApiOperationTransformer
{
    private static readonly string[] FirstNames =
        ["Sarah", "James", "Maria", "David", "Emily", "Michael", "Jessica", "Robert", "Ashley", "William",
         "Jennifer", "Daniel", "Linda", "Christopher", "Patricia", "Matthew", "Barbara", "Andrew", "Susan", "Joshua",
         "Karen", "Anthony", "Nancy", "Mark", "Lisa", "Steven", "Betty", "Paul", "Margaret", "Kevin"];

    private static readonly string[] LastNames =
        ["Mitchell", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
         "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin",
         "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson"];

    private static readonly string[] Streets =
        ["Oak Street", "Maple Avenue", "Cedar Lane", "Pine Drive", "Elm Boulevard",
         "Birch Road", "Walnut Court", "Spruce Way", "Aspen Circle", "Willow Terrace"];

    private static readonly string[] Cities =
        ["Denver", "Boulder", "Aurora", "Lakewood", "Fort Collins",
         "Colorado Springs", "Pueblo", "Thornton", "Arvada", "Westminster"];

    private static readonly string[] States = ["CO", "UT", "WY", "NE", "KS", "NM", "AZ", "TX", "CA", "WA"];

    private static readonly string[] ZipCodes =
        ["80202", "80301", "80012", "80226", "80525", "80903", "81001", "80229", "80002", "80031"];

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var path = context.Description.RelativePath ?? "";

        if (path.EndsWith("patients/batch-import"))
        {
            SetRequestBodyExample(operation, GeneratePatients(10));
        }
        else if (path.EndsWith("patients/bulk-import"))
        {
            SetRequestBodyExample(operation, GenerateBulkPayload(3000));
        }

        return Task.CompletedTask;
    }

    private static void SetRequestBodyExample(OpenApiOperation operation, JsonNode example)
    {
        if (operation.RequestBody?.Content == null) return;

        foreach (var (_, mediaType) in operation.RequestBody.Content)
        {
            mediaType.Example = example;
        }
    }

    private static JsonNode GeneratePatients(int count)
    {
        var patients = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            patients.Add(GeneratePatient(i));
        }
        return new JsonObject { ["patients"] = patients };
    }

    private static JsonNode GenerateBulkPayload(int count)
    {
        var patients = new JsonArray();
        for (var i = 0; i < count; i++)
        {
            patients.Add(GeneratePatient(i));
        }
        return new JsonObject
        {
            ["patients"] = patients,
            ["batchSize"] = 5000
        };
    }

    private static JsonNode GeneratePatient(int index)
    {
        var firstName = FirstNames[index % FirstNames.Length];
        var lastName = LastNames[(index * 7) % LastNames.Length]; // offset to avoid repeating combos

        return new JsonObject
        {
            ["firstName"] = firstName,
            ["lastName"] = lastName,
            ["dateOfBirth"] = DateOnly.FromDateTime(
                DateTime.UtcNow.AddYears(-25 - (index % 50)).AddDays(-(index % 365)))
                .ToString("yyyy-MM-dd"),
            ["email"] = $"{firstName.ToLower()}.{lastName.ToLower()}.{index + 10000}@example.com",
            ["phoneNumber"] = $"{200 + (index % 800)}-{100 + (index % 900)}-{1000 + (index % 9000)}",
            ["street"] = $"{100 + index} {Streets[index % Streets.Length]}",
            ["city"] = Cities[index % Cities.Length],
            ["state"] = States[index % States.Length],
            ["zipCode"] = ZipCodes[index % ZipCodes.Length],
            ["externalId"] = $"EXT-BATCH-{index + 1:D5}"
        };
    }
}
