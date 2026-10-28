using Billing.Application.Accounting;

namespace Billing.Tests;

public class PlaceOfSupplyTests
{
    [Fact]
    public void Company_state_prefers_explicit_code_then_name_then_gstin()
    {
        Assert.Equal("36", PlaceOfSupply.CompanyStateCode("36", "Karnataka", "29AAAAA0000A1Z5"));
        Assert.Equal("36", PlaceOfSupply.CompanyStateCode(null, "Telangana", "29AAAAA0000A1Z5"));
        Assert.Equal("29", PlaceOfSupply.CompanyStateCode(null, null, "29AAAAA0000A1Z5"));
        Assert.Null(PlaceOfSupply.CompanyStateCode(null, null, null));
    }

    [Fact]
    public void Placeholder_company_gstin_does_not_override_the_state_name()
    {
        // 22AAAAA0000A1Z5 says Chhattisgarh but the configured state is Telangana.
        Assert.Equal("36", PlaceOfSupply.CompanyStateCode(null, "Telangana", "22AAAAA0000A1Z5"));
    }

    [Fact]
    public void Party_state_prefers_explicit_code_then_gstin_then_name()
    {
        Assert.Equal("27", PlaceOfSupply.PartyStateCode("27", "Goa", "29AAAAA0000A1Z5"));
        Assert.Equal("29", PlaceOfSupply.PartyStateCode(null, "Goa", "29aaaaa0000a1z5"));
        Assert.Equal("30", PlaceOfSupply.PartyStateCode(null, "Goa", null));
        Assert.Null(PlaceOfSupply.PartyStateCode(" ", null, null));
    }

    [Fact]
    public void Invalid_state_code_is_ignored()
    {
        Assert.Equal("30", PlaceOfSupply.PartyStateCode("99", "Goa", null));
    }

    [Fact]
    public void Unknown_party_state_defaults_to_company_state_and_is_intra_state()
    {
        var supply = PlaceOfSupply.Resolve(partyStateCode: null, companyStateCode: "36");

        Assert.Equal("36", supply);
        Assert.False(PlaceOfSupply.IsInterState(supply, "36"));
    }

    [Fact]
    public void Different_states_are_inter_state()
    {
        Assert.True(PlaceOfSupply.IsInterState("27", "36"));
        Assert.False(PlaceOfSupply.IsInterState("36", "36"));
    }

    [Fact]
    public void Missing_company_state_is_never_inter_state()
    {
        Assert.False(PlaceOfSupply.IsInterState("27", null));
    }
}