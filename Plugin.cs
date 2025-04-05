using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Rules;

namespace TurnTimer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}


[HarmonyPatch(typeof(LocalGameStateView), nameof(LocalGameStateView.OnTurnStarted), typeof(Player))]
internal class StartTurnPatch
{
    private static void Prefix(Player targetPlayer)
    {
        Plugin.Log.LogInfo($"Starting turn for player {targetPlayer.P_ID}:{targetPlayer.P_Username} ({targetPlayer.P_PlayerType}).");
    }
}
