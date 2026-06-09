using FilterIcs;
using Xunit;

namespace FilterIcs.Tests;

public class NameExtractionTests
{
    [Theory]
    [InlineData("John Doe (Folga - 11 dias)", "John Doe")]
    [InlineData("Richard Doe (Folga - 15 dias)", "Richard Doe")]
    [InlineData("Janet Doe", "Janet Doe")]
    [InlineData("  Trimmed Name  (Folga)", "Trimmed Name")]
    [InlineData("Renée Doe (Férias)", "Renée Doe")]
    public void Extract_returns_name_prefix(string summary, string expected)
    {
        Assert.Equal(expected, Names.Extract(summary));
    }
}
