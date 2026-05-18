namespace OpenRithmic;

public sealed record Account(string FcmId, string IbId, string AccountId, string? Name = null)
{
    public string Display => string.IsNullOrEmpty(Name)
        ? $"{FcmId}/{IbId}/{AccountId}"
        : $"{AccountId} ({Name})";
}
