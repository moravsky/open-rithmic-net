using OpenRithmic.Internal;

namespace OpenRithmic.Client.Tests;

public class IgnorableExtTests
{
    [Fact]
    public void ToUtc_converts_unix_seconds_and_microseconds()
    {
        // 2024-01-02T03:04:05.000123Z -> ssboe = 1704164645, usecs = 123
        var ts = IgnorableExt.ToUtc(1704164645, 123);

        var expected = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)
            .AddMicroseconds(123);
        Assert.Equal(expected, ts);
    }

    [Fact]
    public void ToUtc_at_unix_epoch()
    {
        var ts = IgnorableExt.ToUtc(0, 0);
        Assert.Equal(DateTimeOffset.UnixEpoch, ts);
    }
}
