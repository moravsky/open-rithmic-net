namespace OpenRithmic.Client.Tests;

public class AccountTests
{
    [Fact]
    public void Display_uses_FcmId_IbId_AccountId_when_name_is_missing()
    {
        var account = new Account("Fcm1", "Ib1", "A123");
        Assert.Equal("Fcm1/Ib1/A123", account.Display);
    }

    [Fact]
    public void Display_uses_AccountId_and_name_when_name_is_present()
    {
        var account = new Account("Fcm1", "Ib1", "A123", "DemoBox");
        Assert.Equal("A123 (DemoBox)", account.Display);
    }

    [Fact]
    public void Display_falls_back_to_triplet_when_name_is_empty()
    {
        var account = new Account("Fcm1", "Ib1", "A123", "");
        Assert.Equal("Fcm1/Ib1/A123", account.Display);
    }
}
