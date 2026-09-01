using RBBH.ConnectedParties.Exceptions.Custom;

namespace UnitTests.Exceptions
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void ValidationException_PopulatesErrorsDictionary()
        {
            var errors = new Dictionary<string, string[]>
            {
                ["Jmbg"] = new[] { "JMBG must be numeric", "JMBG length must be 13" },
                ["FullName"] = new[] { "Full name is required" }
            };

            var ex = new ValidationException("Validation failed", errors);

            Assert.Equal("Validation failed", ex.Message);
            Assert.NotNull(ex.Errors);
            Assert.Equal(2, ex.Errors.Count);
            Assert.Contains("Jmbg", ex.Errors.Keys);
        }
    }
}
