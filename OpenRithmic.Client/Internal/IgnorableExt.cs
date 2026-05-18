using com.omnesys.rapi;

namespace OpenRithmic.Internal;

internal static class IgnorableExt
{
    public static double? AsNullable(this Ignorable<double> v) => v.Use ? v.Value : null;
    public static int?    AsNullable(this Ignorable<int>    v) => v.Use ? v.Value : null;
    public static long?   AsNullable(this Ignorable<long>   v) => v.Use ? v.Value : null;

    public static DateTimeOffset ToUtc(double ssboe, int usecs) =>
        DateTimeOffset.FromUnixTimeSeconds((long)ssboe).AddMicroseconds(usecs);
}
