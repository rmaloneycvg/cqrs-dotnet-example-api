namespace ClinicApi.Domain.ValueObjects;

public record Address(
    string Street,
    string City,
    string State,
    string ZipCode,
    string? Country = "US");

public record PhoneNumber(string Number, PhoneNumberType Type = PhoneNumberType.Mobile);

public enum PhoneNumberType
{
    Mobile,
    Home,
    Work,
    Fax
}

public record Money(decimal Amount, string Currency = "USD")
{
    public static Money Zero => new(0m);
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot add {a.Currency} and {b.Currency}");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Cannot subtract {a.Currency} and {b.Currency}");
        return new Money(a.Amount - b.Amount, a.Currency);
    }
}

public record InsuranceInfo(
    string ProviderId,
    string ProviderName,
    string PolicyNumber,
    string GroupNumber,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate = null);
