using RBBH.ConnectedParties.BL.Services;
using RBBH.ConnectedParties.DL.DTO.Limiti;
using UnitTests.Mocks.DB.Limiti;

namespace UnitTests.Services.LimitServiceTests
{
    /// <summary>
    /// Unit testovi za LimitService — CRUD operacije i validacije.
    /// </summary>
    public class LimitServiceTests : IClassFixture<LimitFixture>
    {
        private readonly LimitService _service;
        private readonly LimitFixture _fixture;

        public LimitServiceTests(LimitFixture fixture)
        {
            _fixture = fixture;
            _service = new LimitService(fixture._dbContext);
        }

        // GetAll

        [Fact]
        public async Task GetAll_ReturnsSuccess_WithSeededData()
        {
            var result = await _service.GetAll();
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Count >= 2);
        }

        // GetByID

        [Fact]
        public async Task GetByID_WithValidId_ReturnsLimit()
        {
            var result = await _service.GetByID(_fixture.ValidLimitId);
            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Value);
            Assert.Equal(_fixture.ValidLimitId, result.Value.Id);
        }

        [Fact]
        public async Task GetByID_WithInvalidId_ReturnsValidationError()
        {
            var result = await _service.GetByID(0);
            Assert.True(result.IsValidationError);
            Assert.Equal("ID nije validan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task GetByID_WithNonExistentId_ReturnsNotFoundError()
        {
            var result = await _service.GetByID(99999);
            Assert.True(result.IsNotFoundError);
        }

        // Create - validni scenariji

        [Fact]
        public async Task Create_WithValidData_ReturnsSuccess()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "Novi limit",
                TipLimita = "Regulatorni",
                IznosLimita = 500_000m,
                Utilizacija = 50_000m,
                RegulatorniKapital = 2_000_000m,
                OsnovniKapital = 1_000_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Value);
            Assert.Equal("Novi limit", result.Value.Naziv);
            Assert.Equal(450_000m, result.Value.RaspoloziviLimit);
        }

        [Fact]
        public async Task Create_WithKorigovaniLimit_CalculatesRaspoloziviLimitCorrectly()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "Limit s korigovanim",
                TipLimita = "Interni",
                IznosLimita = 1_000_000m,
                Utilizacija = 100_000m,
                KorigovaniLimit = 800_000m,
                RegulatorniKapital = 3_000_000m,
                OsnovniKapital = 2_000_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal(700_000m, result.Value!.RaspoloziviLimit);
        }

        // Create - validacije

        [Fact]
        public async Task Create_WithEmptyNaziv_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Naziv je obavezan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithNazivLongerThan100Chars_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = new string('A', 101),
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
        }

        [Fact]
        public async Task Create_WithEmptyTipLimita_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "Validan naziv",
                TipLimita = "",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Tip limita je obavezan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithNullRegulatorniKapital_UsesZeroUntilCapitalIsEnteredSeparately()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "Validan naziv",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = null,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal(0, result.Value!.RegulatorniKapital);
        }

        [Fact]
        public async Task Create_WithNullOsnovniKapital_UsesZeroUntilCapitalIsEnteredSeparately()
        {
            var dto = new CreateLimitDTO
            {
                Naziv = "Validan naziv",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = null
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal(0, result.Value!.OsnovniKapital);
        }

        // Update

        [Fact]
        public async Task Update_WithValidData_ReturnsSuccess()
        {
            var dto = new UpdateLimitDTO
            {
                Naziv = "Izmijenjeni naziv",
                TipLimita = "Regulatorni",
                IznosLimita = 900_000m,
                Utilizacija = 100_000m,
                RegulatorniKapital = 5_000_000m,
                OsnovniKapital = 3_000_000m
            };

            var result = await _service.Update(_fixture.ValidLimitId2, dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal("Izmijenjeni naziv", result.Value!.Naziv);
            Assert.Equal(800_000m, result.Value.RaspoloziviLimit);
        }

        [Fact]
        public async Task Update_WithInvalidId_ReturnsValidationError()
        {
            var dto = new UpdateLimitDTO
            {
                Naziv = "Naziv",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Update(0, dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("ID nije validan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Update_WithNonExistentId_ReturnsNotFoundError()
        {
            var dto = new UpdateLimitDTO
            {
                Naziv = "Naziv",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Update(99999, dto, "test.user");

            Assert.True(result.IsNotFoundError);
        }

        [Fact]
        public async Task Update_WithEmptyNaziv_ReturnsValidationError()
        {
            var dto = new UpdateLimitDTO
            {
                Naziv = "   ",
                TipLimita = "Regulatorni",
                IznosLimita = 100_000m,
                Utilizacija = 10_000m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };

            var result = await _service.Update(_fixture.ValidLimitId, dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Naziv je obavezan.", result.ExceptionMessage);
        }

        // Delete

        [Fact]
        public async Task Delete_WithValidId_ReturnsSuccess()
        {
            var createDto = new CreateLimitDTO
            {
                Naziv = "Limit za brisanje",
                TipLimita = "Interni",
                IznosLimita = 100_000m,
                Utilizacija = 0m,
                RegulatorniKapital = 500_000m,
                OsnovniKapital = 300_000m
            };
            var created = await _service.Create(createDto, "test.user");
            var idZaBrisanje = created.Value!.Id;

            var result = await _service.Delete(idZaBrisanje);

            Assert.True(result.IsSuccessful);
            Assert.True(result.Value);

            var getResult = await _service.GetByID(idZaBrisanje);
            Assert.True(getResult.IsNotFoundError);
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsValidationError()
        {
            var result = await _service.Delete(-1);
            Assert.True(result.IsValidationError);
            Assert.Equal("ID nije validan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Delete_WithNonExistentId_ReturnsNotFoundError()
        {
            var result = await _service.Delete(99999);
            Assert.True(result.IsNotFoundError);
        }
    }
}
