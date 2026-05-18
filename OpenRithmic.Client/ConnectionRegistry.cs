using System.Reflection;
using System.Text.RegularExpressions;

namespace OpenRithmic;

public static class ConnectionRegistry
{
    private const string ResourcePrefix = "OpenRithmic.RApiConfig.";
    private const string ResourceSuffix = "_connection_params.txt";

    private static readonly Lazy<IReadOnlyList<RithmicConnection>> _all = new(Load);

    public static IReadOnlyList<RithmicConnection> All => _all.Value;

    public static RithmicConnection? Find(string systemSlashGateway)
    {
        var parts = systemSlashGateway.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return null;

        return All.FirstOrDefault(c =>
            string.Equals(c.SystemName, parts[0], StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.GatewayName, parts[1], StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<RithmicConnection> Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        var names = asm.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                     && n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            .ToArray();

        var result = new List<RithmicConnection>(names.Length);
        foreach (var name in names)
        {
            using var stream = asm.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Missing resource: {name}");
            using var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();
            if (TryParse(text, out var connection))
                result.Add(connection);
        }

        return result
            .OrderBy(c => c.SystemName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.GatewayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static bool TryParse(string text, out RithmicConnection connection)
    {
        connection = null!;

        var systemName  = MatchValue(text, @"^\s*System Name\s*:\s*(.+?)\s*$");
        var gatewayName = MatchValue(text, @"^\s*Gateway Name\s*:\s*(.+?)\s*$");
        if (systemName is null || gatewayName is null)
            return false;

        var dotNetIdx = text.IndexOf("For .NET Rithmic APIs", StringComparison.Ordinal);
        if (dotNetIdx < 0)
            return false;
        var dotNet = text[dotNetIdx..];

        var adm     = MatchValue(dotNet, @"REngineParams\.AdmCnnctPt\s*:\s*(\S+)");
        var dmn     = MatchValue(dotNet, @"REngineParams\.DmnSrvrAddr\s*:\s*(\S+)");
        var domain  = MatchValue(dotNet, @"REngineParams\.DomainName\s*:\s*(\S+)");
        var lic     = MatchValue(dotNet, @"REngineParams\.LicSrvrAddr\s*:\s*(\S+)");
        var locBrok = MatchValue(dotNet, @"REngineParams\.LocBrokAddr\s*:\s*(\S+)");
        var logger  = MatchValue(dotNet, @"REngineParams\.LoggerAddr\s*:\s*(\S+)");

        var repoIdx = dotNet.IndexOf("loginRepository", StringComparison.Ordinal);
        var loginIdx = dotNet.IndexOf("REngine::login() Params", StringComparison.Ordinal);
        if (repoIdx < 0 || loginIdx < 0 || repoIdx >= loginIdx)
            return false;

        var repoSection = dotNet[repoIdx..loginIdx];
        var loginSection = dotNet[loginIdx..];

        var repo = MatchValue(repoSection, @"sCnnctPt\s*:\s*(\S+)");
        var md   = MatchValue(loginSection, @"sMdCnnctPt\s*:\s*(\S+)");
        var ih   = MatchValue(loginSection, @"sIhCnnctPt\s*:\s*(\S+)");
        var ts   = MatchValue(loginSection, @"sTsCnnctPt\s*:\s*(\S+)");
        var pnl  = MatchValue(loginSection, @"sPnLCnnctPt\s*:\s*(\S+)");

        if (adm is null || dmn is null || domain is null || lic is null || locBrok is null ||
            logger is null || repo is null || md is null || ih is null || ts is null || pnl is null)
            return false;

        connection = new RithmicConnection(
            SystemName: systemName,
            GatewayName: gatewayName,
            AdmCnnctPt: adm,
            DomainName: domain,
            DmnSrvrAddr: dmn,
            LicSrvrAddr: lic,
            LocBrokAddr: locBrok,
            LoggerAddr: logger,
            RepositoryCnnctPt: repo,
            MdCnnctPt: md,
            IhCnnctPt: ih,
            TsCnnctPt: ts,
            PnLCnnctPt: pnl);
        return true;
    }

    private static string? MatchValue(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }
}
