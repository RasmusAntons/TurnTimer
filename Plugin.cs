using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Rules;
using UnityEngine;

namespace TurnTimer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
    internal new static ManualLogSource Log;
    internal static List<Tuple<string, int>> Moves;
    internal static string CurrentTurnUserID;
    internal static long CurrentTurnStartTime;

    public override void Load() {
        Log = base.Log;
        Moves = [];

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        ClassInjector.RegisterTypeInIl2Cpp<OnScreenTimer>();
        
        harmony.PatchAll();
    }

    public static void StartMove(string id) {
        if (CurrentTurnUserID != null) {
            Log.LogWarning($"Previous Move by player {CurrentTurnUserID} was never ended.");
        }
        CurrentTurnUserID = id;
        CurrentTurnStartTime = DateTime.UtcNow.Ticks;
    }

    public static void EndMove(string id) {
        if (CurrentTurnUserID != id) {
            Log.LogWarning($"The current move is by player {CurrentTurnUserID} and not {id}.");
            return;
        }
        CurrentTurnUserID = null;
        var endTime = DateTime.UtcNow.Ticks;
        Moves.Add(new Tuple<string, int>(id, (int)(endTime - CurrentTurnStartTime)));
        Log.LogInfo($"Player {id}'s move took {endTime - CurrentTurnStartTime} ticks.");
    }
}

[HarmonyPatch(typeof(LocalGameStateView), nameof(LocalGameStateView.OnTurnStarted), typeof(Player))]
internal class OnTurnStart {
    private static void Prefix(Player targetPlayer) {
        Plugin.Log.LogInfo($"Starting turn for player {targetPlayer.P_ID}:{targetPlayer.P_Username} ({targetPlayer.P_PlayerType}).");
        Plugin.StartMove(targetPlayer.P_ID);
    }
}

[HarmonyPatch(typeof(LocalGameStateView), nameof(LocalGameStateView.OnTurnFinished), typeof(Player))]
internal class OnTurnEnd {
    private static void Prefix(Player targetPlayer) {
        Plugin.Log.LogInfo($"Ending turn for player {targetPlayer.P_ID}:{targetPlayer.P_Username} ({targetPlayer.P_PlayerType}).");
        Plugin.EndMove(targetPlayer.P_ID);
    }
}

public class OnScreenTimer : MonoBehaviour {
    private void Update() {
        throw new NotImplementedException();
    }
}
