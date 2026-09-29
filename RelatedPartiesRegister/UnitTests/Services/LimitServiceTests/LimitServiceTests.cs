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

        [Fact]
        public async Task GetAll_FilteredByLegalEntityId_ReturnsOnlyThatClient()
        {
            var result = await _service.GetAll(legalEntityId: _fixture.ValidLegalEntityId);
            Assert.True(result.IsSuccessful);
            Assert.All(result.Value, item => Assert.Equal(_fixture.ValidLegalEntityId, item.LegalEntityId));
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
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita,
                OdobreniLimit = 500_000m,
                MaksimalnaOcekivanaUtilizacija = 50_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.NotNull(result.Value);
            Assert.Equal(_fixture.ValidLegalEntityId, result.Value.LegalEntityId);
            Assert.Equal(LimitFixture.ValidTipLimita, result.Value.TipLimita);
            Assert.Equal(500_000m, result.Value.OdobreniLimit);
        }

        [Fact]
        public async Task Create_LowercaseTipLimita_IsNormalizedToUppercase()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita.ToLower(),
                OdobreniLimit = 100_000m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal(LimitFixture.ValidTipLimita.ToUpper(), result.Value!.TipLimita);
        }

        // Create - validacije

        [Fact]
        public async Task Create_WithEmptyLegalEntityId_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = Guid.Empty,
                TipLimita = LimitFixture.ValidTipLimita
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Pravno lice je obavezno.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithNonExistentLegalEntityId_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = Guid.NewGuid(),
                TipLimita = LimitFixture.ValidTipLimita
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Odabrano pravno lice ne postoji.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithEmptyTipLimita_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = ""
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Tip limita je obavezan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithTipLimitaNotInCodeList_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = "NEPOSTOJECI"
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
        }

        [Fact]
        public async Task Create_WithInactiveTipLimita_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.InactiveTipLimita
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
        }

        [Fact]
        public async Task Create_WithNegativeOdobreniLimit_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita,
                OdobreniLimit = -1m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Odobreni limit ne može biti negativan.", result.ExceptionMessage);
        }

        [Fact]
        public async Task Create_WithNegativeMaksimalnaOcekivanaUtilizacija_ReturnsValidationError()
        {
            var dto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita,
                MaksimalnaOcekivanaUtilizacija = -1m
            };

            var result = await _service.Create(dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Maksimalna očekivana utilizacija ne može biti negativna.", result.ExceptionMessage);
        }

        // Update

        [Fact]
        public async Task Update_WithValidData_ReturnsSuccess()
        {
            var dto = new UpdateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId2,
                TipLimita = LimitFixture.ValidTipLimita2,
                OdobreniLimit = 900_000m,
                MaksimalnaOcekivanaUtilizacija = 100_000m
            };

            var result = await _service.Update(_fixture.ValidLimitId2, dto, "test.user");

            Assert.True(result.IsSuccessful);
            Assert.Equal(900_000m, result.Value!.OdobreniLimit);
        }

        [Fact]
        public async Task Update_WithInvalidId_ReturnsValidationError()
        {
            var dto = new UpdateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita
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
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita
            };

            var result = await _service.Update(99999, dto, "test.user");

            Assert.True(result.IsNotFoundError);
        }

        [Fact]
        public async Task Update_WithNonExistentLegalEntityId_ReturnsValidationError()
        {
            var dto = new UpdateLimitDTO
            {
                LegalEntityId = Guid.NewGuid(),
                TipLimita = LimitFixture.ValidTipLimita
            };

            var result = await _service.Update(_fixture.ValidLimitId, dto, "test.user");

            Assert.True(result.IsValidationError);
            Assert.Equal("Odabrano pravno lice ne postoji.", result.ExceptionMessage);
        }

        // Delete

        [Fact]
        public async Task Delete_WithValidId_ReturnsSuccess()
        {
            var createDto = new CreateLimitDTO
            {
                LegalEntityId = _fixture.ValidLegalEntityId,
                TipLimita = LimitFixture.ValidTipLimita
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
