using FileStorage.Common.Storage;

namespace FileStorage.UnitTests;

public class ShortCodeGeneratorTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Generate_ReturnsExpectedLength(int length)
    {
        var code = ShortCodeGenerator.Generate(length);

        Assert.Equal(length, code.Length);
    }

    [Fact]
    public void Generate_OnlyContainsBase62Characters()
    {
        var code = ShortCodeGenerator.Generate(64);

        Assert.All(code, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
    }

    [Fact]
    public void Generate_ProducesUniqueCodesInBulk()
    {
        var set = new HashSet<string>();
        for (var i = 0; i < 10_000; i++)
        {
            Assert.True(set.Add(ShortCodeGenerator.Generate(5)));
        }
    }

    [Fact]
    public void Generate_Throws_ForInvalidLength()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShortCodeGenerator.Generate(0));
    }
}