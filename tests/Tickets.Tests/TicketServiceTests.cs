using Moq;
using Tickets.Application.Abstractions;
using Tickets.Application.Common;
using Tickets.Application.Dtos;
using Tickets.Application.Services;
using Tickets.Domain.Entities;
using Tickets.Domain.Enums;
using Xunit;

namespace Tickets.Tests;

/// <summary>
/// Los servicios son testeables porque dependen de INTERFACES (inyección de dependencias),
/// a diferencia del legacy con métodos estáticos y acceso directo a BD.
/// </summary>
public class TicketServiceTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ICatalogRepository> _catalogRepo = new();
    private readonly Mock<IUserDirectoryRepository> _directory = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private TicketService CreateSut()
    {
        _currentUser.SetupGet(x => x.UserCode).Returns("demo");
        _currentUser.SetupGet(x => x.DepartmentCode).Returns("CC-01");
        _directory.Setup(x => x.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return new TicketService(_ticketRepo.Object, _catalogRepo.Object, _directory.Object, _currentUser.Object);
    }

    [Fact]
    public async Task CreateAsync_WithoutDescription_Throws()
    {
        var sut = CreateSut();
        var req = new CreateTicketRequest { TicketTypeId = 1, Description = "  " };

        await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(req));
    }

    [Fact]
    public async Task CreateAsync_QualityAreaWithoutAmount_Throws()
    {
        var sut = CreateSut();
        _catalogRepo.Setup(x => x.GetTicketTypeAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketType { Id = 5, Name = "No conformidad", AreaCode = "CAL" });

        var req = new CreateTicketRequest
        {
            TicketTypeId = 5,
            Description = "Falla",
            QualityDepartment = "Producción",
            Quantity = 3
            // Amount ausente -> debe fallar
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sut.CreateAsync(req));
        Assert.Contains("monto", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_SetsDefaultsAndInserts()
    {
        var sut = CreateSut();
        _catalogRepo.Setup(x => x.GetTicketTypeAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TicketType { Id = 2, Name = "Soporte", AreaCode = "SIS", DefaultResponsibleUserCode = "blozano" });
        Ticket? captured = null;
        _ticketRepo.Setup(x => x.InsertAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()))
            .Callback<Ticket, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync(99);

        var id = await sut.CreateAsync(new CreateTicketRequest { TicketTypeId = 2, Description = "Requiero acceso" });

        Assert.Equal(99, id);
        Assert.NotNull(captured);
        Assert.Equal("demo", captured!.RequesterUserCode);
        Assert.Equal("blozano", captured.ResponsibleUserCode);
        Assert.Equal(TicketStatus.Open, captured.Status);
        Assert.Equal("S", captured.Category);
    }

    [Fact]
    public async Task UpdateStatusAsync_InProgressWithoutEstimate_Throws()
    {
        var sut = CreateSut();
        _ticketRepo.Setup(x => x.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ticket { Id = 7 });

        var req = new UpdateTicketStatusRequest { TicketId = 7, Status = TicketStatus.InProgress };

        await Assert.ThrowsAsync<ValidationException>(() => sut.UpdateStatusAsync(req));
    }

    [Fact]
    public async Task UpdateResponsibleAsync_BlankResponsible_Throws()
    {
        var sut = CreateSut();
        var req = new UpdateResponsibleRequest { TicketId = 1, ResponsibleUserCode = "  " };

        await Assert.ThrowsAsync<ValidationException>(() => sut.UpdateResponsibleAsync(req));
    }
}
