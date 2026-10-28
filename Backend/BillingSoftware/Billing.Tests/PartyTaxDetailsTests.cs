using Billing.Application.Accounting;

namespace Billing.Tests;

public class PartyTaxDetailsTests
{
    [Fact]
    public void Nothing_given_is_fine_for_a_walk_in_customer()
    {
        var info = PartyTaxDetails.Normalize(null, null, null);

        Assert.Null(info.State);
        Assert.Null(info.StateCode);
        Assert.Null(info.Gstin);
    }

    [Fact]
    public void Blank_strings_count_as_nothing()
    {
        var info = PartyTaxDetails.Normalize("  ", " ", "");
        Assert.Equal(new PartyTaxInfo(null, null, null), info);
    }

    [Fact]
    public void State_name_resolves_code_and_official_name()
    {
        var info = PartyTaxDetails.Normalize(" telangana ", null, null);

        Assert.Equal("Telangana", info.State);
        Assert.Equal("36", info.StateCode);
    }

    [Fact]
    public void Gstin_alone_supplies_the_state()
    {
        var info = PartyTaxDetails.Normalize(null, null, "27aapfu0939f1zv");

        Assert.Equal("27AAPFU0939F1ZV", info.Gstin);
        Assert.Equal("27", info.StateCode);
        Assert.Equal("Maharashtra", info.State);
    }

    [Fact]
    public void Gstin_that_contradicts_the_chosen_state_is_rejected()
    {
        var ex = Assert.Throws<PartyTaxException>(() =>
            PartyTaxDetails.Normalize("Telangana", null, "27AAPFU0939F1ZV"));

        Assert.Contains("Maharashtra", ex.Message);
        Assert.Contains("Telangana", ex.Message);
    }

    [Fact]
    public void Matching_gstin_and_state_are_accepted()
    {
        var info = PartyTaxDetails.Normalize("Maharashtra", "27", "27AAPFU0939F1ZV");
        Assert.Equal("27", info.StateCode);
    }

    [Theory]
    [InlineData("27AAPFU0939F1")]
    [InlineData("ABCDEFGHIJKLMNO")]
    public void Malformed_gstin_is_rejected(string gstin) =>
        Assert.Throws<PartyTaxException>(() => PartyTaxDetails.Normalize(null, null, gstin));

    [Fact]
    public void Unknown_state_code_is_rejected() =>
        Assert.Throws<PartyTaxException>(() => PartyTaxDetails.Normalize(null, "99", null));

    [Fact]
    public void Unrecognised_state_name_is_rejected() =>
        Assert.Throws<PartyTaxException>(() => PartyTaxDetails.Normalize("Atlantis", null, null));
}