using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class NameExtractionTests
{
    [Theory]
    [InlineData("Pedro Fernandes (Folga - 11 dias)", "Pedro Fernandes")]
    [InlineData("Luciano Lizzoni (Folga - 15 dias)", "Luciano Lizzoni")]
    [InlineData("Maria Silva", "Maria Silva")]
    [InlineData("  Trimmed Name  (Folga)", "Trimmed Name")]
    [InlineData("José da Conceição (Férias)", "José da Conceição")]
    public void Extract_returns_name_prefix(string summary, string expected)
    {
        Assert.Equal(expected, Names.Extract(summary));
    }
}
