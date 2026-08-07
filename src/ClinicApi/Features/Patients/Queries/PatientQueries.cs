namespace ClinicApi.Features.Patients.Queries;

// Get a single patient by ID
public record GetPatientById(Guid Id);

// List patients with pagination
public record ListPatients(int Page = 1, int PageSize = 20, string? Status = null, string? SearchTerm = null);

// Query response models (designed for future GraphQL consumption)
public record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber,
    string Status,
    string City,
    string State,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record PatientListResponse(
    List<PatientDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
