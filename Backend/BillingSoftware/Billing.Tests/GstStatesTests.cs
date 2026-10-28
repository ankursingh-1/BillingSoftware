using Billing.Application.Accounting;

namespace Billing.Tests;

public class GstStatesTests
{
    [Theory]
    [InlineData("Telangana", "36")]
    [InlineData("  telangana ", "36")]
    [InlineData("Tamil Nadu", "33")]
    [InlineData("Jammu & Kashmir", "01")]
    [InlineData("Orissa", "21")]
    [InlineData("Maharashtra", "27")]
    public void State_name_maps_to_code(string name, string code) =>
        Assert.Equal(code, GstStates.CodeFromName(name));

    [Theory]
    [InlineData("Atlantis")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_state_name_gives_no_code(string? name) =>
        Assert.Null(GstStates.CodeFromName(name));

    [Fact]
    public void Every_state_code_round_trips_through_its_name()
    {
        foreach (var (code, name) in GstStates.All)
        {
            Assert.Equal(code, GstStates.CodeFromName(name));
            Assert.Equal(name, GstStates.NameOf(code));
        }
    }

    [Theory]
    [InlineData("27AAPFU0939F1ZV", true)]
    [InlineData("22AAAAA0000A1Z5", true)]
    [InlineData("27aapfu0939f1zv", false)]  // must be normalised first
    [InlineData("99AAPFU0939F1ZV", false)]  // unknown state code
    [InlineData("27AAPFU0939F1AV", false)]  // 14th character must be Z
    [InlineData("27AAPFU0939F1Z", false)]   // too short
    public void Gstin_format(string gstin, bool valid) =>
        Assert.Equal(valid, GstStates.IsValidGstinFormat(gstin));

    [Fact]
    public void Gstin_prefix_is_the_state_code() =>
        Assert.Equal("27", GstStates.CodeFromGstin("27AAPFU0939F1ZV"));

    [Fact]
    public void Normalize_gstin_trims_and_uppercases()
    {
        Assert.Equal("27AAPFU0939F1ZV", GstStates.NormalizeGstin(" 27aapfu0939f1zv "));
        Assert.Null(GstStates.NormalizeGstin("   "));
    }
}
