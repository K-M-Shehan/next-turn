using FluentAssertions;
using NextTurn.Application.Service.Queries.ListServices;

namespace NextTurn.UnitTests.Application.Service;

public sealed class ListServicesQueryValidatorTests
{
    private readonly ListServicesQueryValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithValidQuery_HasNoErrors()
    {
        var query = new ListServicesQuery(Guid.NewGuid(), true, 1, 20);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidPageSize_ReturnsError()
    {
        var query = new ListServicesQuery(Guid.NewGuid(), true, 1, 0);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ListServicesQuery.PageSize));
    }
}
