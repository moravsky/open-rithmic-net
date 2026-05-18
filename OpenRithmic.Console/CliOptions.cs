namespace OpenRithmic.ConsoleApp;

internal sealed class CliOptions
{
    public string Connection { get; set; } = "Rithmic Test/Orangeburg";
    public string? User { get; set; }
    public string? Password { get; set; }
    public string? CertFile { get; set; }
    public bool ListConnections { get; set; }
    public bool Help { get; set; }
    public bool NoMarketData { get; set; }

    public static CliOptions Parse(string[] args)
    {
        var opts = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    opts.Help = true;
                    break;
                case "--list-connections":
                    opts.ListConnections = true;
                    break;
                case "--connection":
                    opts.Connection = NextArg(args, ref i, "--connection");
                    break;
                case "--user":
                    opts.User = NextArg(args, ref i, "--user");
                    break;
                case "--password":
                    opts.Password = NextArg(args, ref i, "--password");
                    break;
                case "--cert":
                    opts.CertFile = NextArg(args, ref i, "--cert");
                    break;
                case "--no-md":
                    opts.NoMarketData = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }
        return opts;
    }

    private static string NextArg(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
            throw new ArgumentException($"{name} requires a value");
        return args[++i];
    }

    public static string UsageText => """
        Usage:
          OpenRithmic.Console --user <user> --password <password> [options]

        Options:
          --connection <"System/Gateway">  Pick a Rithmic system/gateway (default: "Rithmic Test/Orangeburg")
          --user <user>                    Rithmic login user
          --password <password>            Rithmic login password
          --cert <path>                    Full path to rithmic_ssl_cert_auth_params (sets MML_SSL_CLNT_AUTH_FILE)
          --no-md                          Skip market-data login (PnL+TS only)
          --list-connections               Print all embedded System/Gateway pairs and exit
          -h, --help                       Show this help
        """;
}
