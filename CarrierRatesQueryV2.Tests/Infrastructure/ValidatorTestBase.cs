using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public abstract class ValidatorTestBase
{
    protected static (TValidator validator, AppDbContext db) SetupValidator<TValidator>(Action<IServiceCollection>? additionalServices = null)
        where TValidator : class, IValidator
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var db = new AppDbContext(options);

        var validator = Factory.CreateValidator<TValidator>(s =>
        {
            s.AddScoped(_ => db);
            additionalServices?.Invoke(s);
        });

        return (validator, db);
    }
}
