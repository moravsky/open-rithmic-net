namespace RithmicBalancePnlTradeData;

public sealed record RithmicConnection(
    string SystemName,
    string GatewayName,
    string AdmCnnctPt,
    string DomainName,
    string DmnSrvrAddr,
    string LicSrvrAddr,
    string LocBrokAddr,
    string LoggerAddr,
    string RepositoryCnnctPt,
    string MdCnnctPt,
    string IhCnnctPt,
    string TsCnnctPt,
    string PnLCnnctPt)
{
    public string DisplayName => $"{SystemName}/{GatewayName}";
}
