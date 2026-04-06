using FluentAssertions;
using Moq;
using NextTurn.Application.Common.Interfaces;
using NextTurn.Application.Staff.Commands.CreateStaff;
using NextTurn.Domain.Auth.Entities;
using NextTurn.Domain.Auth.Repositories;
using NextTurn.Domain.Auth.ValueObjects;
using NextTurn.Domain.Common;

namespace NextTurn.UnitTests.Application.Staff;

public sealed class CreateStaffCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ITenantContext> _tenantContextMock = new();

    private readonly CreateStaffCommandHandler _handler;

    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public CreateStaffCommandHandlerTests()
    {
        _tenantContextMock.Setup(x => x.TenantId).Returns(TenantId);
        _handler = new CreateStaffCommandHandler(_userRepositoryMock.Object, _tenantContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesStaffAndAssignments()
    {
        var officeId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.OfficesExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateStaffCommand(
            "Counter Agent",
            "staff@example.com",
            "0711111111",
            new[] { officeId },
            "Counter A",
            TimeSpan.Parse("09:00"),
            TimeSpan.Parse("17:00"));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Counter Agent");
        result.Email.Should().Be("staff@example.com");
        result.OfficeIds.Should().Contain(officeId);

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.ReplaceStaffOfficeAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOfficeMissing_ThrowsDomainException()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(r => r.OfficesExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateStaffCommand(
            "Counter Agent",
            "staff@example.com",
            null,
            new[] { Guid.NewGuid() },
            null,
            null,
            null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("One or more offices were not found for this tenant.");
    }
}
