using System.Text;
using com.omnesys.rapi;

namespace RithmicBalancePnlTradeData.Callbacks;

internal sealed class AdmCallbacksImpl : AdmCallbacks
{
    public override void Alert(AlertInfo oInfo)
    {
        var sb = new StringBuilder();
        sb.Append("[Adm.Alert] ");
        oInfo.Dump(sb);
        Console.Out.Write(sb);
    }
}
