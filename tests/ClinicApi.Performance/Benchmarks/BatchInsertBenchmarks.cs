using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ClinicApi.Domain.Entities;
using ClinicApi.Domain.ValueObjects;
using ClinicApi.Features.Patients.Commands;
using ClinicApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Bogus;

namespace ClinicApi.Performance.Benchmarks;

/// <summary>
/// BenchmarkDotNet micro-benchmarks comparing batch insert strategies.
/// 
/// Run: dotnet run --project tests/ClinicApi.Performance -c Release -- --filter "*BatchInsert*"
/// 
/// Compares:
/// - EF Core AddRange (change-tracked, batched SaveChanges)
/// - EF Core AddRange with chunking (500-row chunks)
/// - Direct Dapper insert (no change tracking)
/// 
/// Note: SqlBulkCopy benchmarks require a real SQL Server instance
/// and are covered in the integration/performance test suite.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class BatchInsertBenchmarks
{
    private List<CreatePatient> _patients = [];

    [Params(100, 500, 1000)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var faker = new Faker();
        _patients = Enumerable.Range(0, BatchSize).Select(_ => new CreatePatient(
            FirstName: faker.Name.FirstName(),
            LastName: faker.Name.LastName(),
            DateOfBirth: DateOnly.FromDateTime(faker.Date.Past(50, DateTime.UtcNow.AddYears(-18))),
            Email: faker.Internet.Email(),
            PhoneNumber: faker.Phone.PhoneNumber("###-###-####"),
            Street: faker.Address.StreetAddress(),
            City: faker.Address.City(),
            State: faker.Address.State(),
            ZipCode: faker.Address.ZipCode("#####")
        )).ToList();
    }

    [Benchmark(Baseline = true)]
    public async Task EfCore_AddRange_SingleSave()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ClinicDbContext(options);

        var entities = _patients.Select(p => new Patient
        {
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            Email = p.Email,
            Phone = new PhoneNumber(p.PhoneNumber),
            Address = new Address(p.Street, p.City, p.State, p.ZipCode)
        }).ToList();

        db.Patients.AddRange(entities);
        await db.SaveChangesAsync();
    }

    [Benchmark]
    public async Task EfCore_AddRange_Chunked500()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ClinicDbContext(options);

        foreach (var chunk in _patients.Chunk(500))
        {
            var entities = chunk.Select(p => new Patient
            {
                FirstName = p.FirstName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Email = p.Email,
                Phone = new PhoneNumber(p.PhoneNumber),
                Address = new Address(p.Street, p.City, p.State, p.ZipCode)
            }).ToList();

            db.Patients.AddRange(entities);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
    }

    [Benchmark]
    public async Task EfCore_AddRange_NoTracking()
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new ClinicDbContext(options);
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        var entities = _patients.Select(p => new Patient
        {
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            Email = p.Email,
            Phone = new PhoneNumber(p.PhoneNumber),
            Address = new Address(p.Street, p.City, p.State, p.ZipCode)
        }).ToList();

        db.Patients.AddRange(entities);
        await db.SaveChangesAsync();
    }
}

/// <summary>
/// To run benchmarks directly:
/// dotnet run --project tests/ClinicApi.Performance -c Release -- --filter "*BatchInsert*"
/// Note: Run outside of the test runner, as a standalone console app.
/// </summary>
