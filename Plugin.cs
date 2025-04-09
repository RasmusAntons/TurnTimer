using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Rules;
using UnityEngine;
using Object = UnityEngine.Object;

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
        
        var go = new GameObject("OnScreenTimer");
        go.AddComponent<OnScreenTimer>();
        Object.DontDestroyOnLoad(go);
    }

    public static void EndMove(string id) {
        if (CurrentTurnUserID != id) {
            Log.LogWarning($"The current move is by player {CurrentTurnUserID} and not {id}.");
            return;
        }
        CurrentTurnUserID = null;
        var endTime = DateTime.UtcNow.Ticks;
        var timeDiff = (int) ((endTime - CurrentTurnStartTime) / TimeSpan.TicksPerMillisecond);
        Moves.Add(new Tuple<string, int>(id, timeDiff));
        Log.LogInfo($"Player {id}'s move took {timeDiff / 1000f:F} s.");
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
        
    }

    private void OnGUI() {
        var text = "No current turn";
        if (Plugin.CurrentTurnUserID != null) {
            var dTime = (DateTime.UtcNow.Ticks - Plugin.CurrentTurnStartTime) / TimeSpan.TicksPerMillisecond;
            text = $"player {Plugin.CurrentTurnUserID}: {dTime / 1000f:F} s";
        }
        GUI.Label(new Rect(10, 10, 200, 30), text);
    }
}
