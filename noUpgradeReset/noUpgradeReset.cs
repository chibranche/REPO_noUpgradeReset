﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using BepInEx;
using BepInEx;
using BepInEx.Configuration; // Added for ConfigFile
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Generic; // Required for IEnumerable in Transpiler
using System.Reflection; // Required for FieldInfo, MethodInfo
using System.Reflection.Emit; // Required for OpCodes, CodeInstruction
using HarmonyLib; // Ensure HarmonyLib is included for transpiler types

namespace NoUpgradeReset
{
    [BepInPlugin("Chibranche.NoUpgradeReset", "NoUpgradeReset", "1.0")] // Renamed GUID and Name
    public class NoUpgradeReset : BaseUnityPlugin // Renamed class
    {
        internal static NoUpgradeReset Instance { get; private set; } = null!; // Renamed class reference
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;
        internal Harmony? Harmony { get; set; }

        // Dictionary to hold our configuration entries (Now belongs to NoUpgradeReset class)
        internal static Dictionary<string, ConfigEntry<bool>> KeepConfigEntries = new Dictionary<string, ConfigEntry<bool>>(); // Renamed for clarity

        // List of keys we want to make configurable (to potentially keep)
        private readonly List<string> configurableStatKeys = new List<string>
        {
            "playerUpgradeSpeed",
            "playerUpgradeStrength",
            "playerUpgradeRange",
            "playerUpgradeThrow",
            "playerUpgradeHealth", 
            "playerUpgradeStamina", 
            "playerUpgradeExtraJump",
            "playerUpgradeLaunch",
            "playerUpgradeMapPlayerCount" 
            // Add other keys from dictionaryOfDictionaries if needed, to support mods for example
        };

        private void Awake()
        {
            Instance = this;

            // --- Configuration Setup ---
            string sectionName = "1. Reset Settings";
            foreach (var key in configurableStatKeys.Distinct()) // Use Distinct() in case of accidental duplicates in the list
            {
                // Create a user-friendly name for the config setting
                string settingName = $"Keep {key} (Do Not Reset)";
                string description = $"Set to true to PREVENT the '{key}' stats dictionary from being reset on new game/run.";
                // Default to true (meaning no reset by default, unless explicitly set to false here)
                KeepConfigEntries[key] = Config.Bind(sectionName, settingName, true, description); // Use renamed dictionary
            }
            // --- End Configuration Setup ---

            // Prevent the plugin from being deleted
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }

        private void Update()
        {
            // Code that runs every frame goes here
        }
    }

    // --- Harmony Patches ---

    [HarmonyPatch(typeof(StatsManager), "ResetAllStats")]
    public static class StatsManager_ResetAllStats_Patch
    {
        // Prefix runs before the original method. Returning false skips the original.
        public static bool Prefix(StatsManager __instance) // __instance is the instance of StatsManager
        {
            NoUpgradeReset.Logger.LogInfo("Executing patched ResetAllStats: Selectively clearing dictionaries based on config."); // Use new class name for Logger

            // Original logic parts that should always run
            __instance.saveFileReady = false;
            ItemManager.instance.ResetAllItems(); // Assuming ItemManager is accessible

            // --- Selectively clear dictionaries based on config ---
            // Assuming the field is named 'dictionaryOfDictionaries'
            foreach (KeyValuePair<string, Dictionary<string, int>> keyValuePair in __instance.dictionaryOfDictionaries)
            {
            // Check if we have a config entry for this key AND if it's set to true (meaning "Keep")
            if (NoUpgradeReset.KeepConfigEntries.TryGetValue(keyValuePair.Key, out var configEntry) && configEntry.Value) // Use new class name and dictionary name
            {
                // Config exists and is set to true (Keep this dictionary)
                NoUpgradeReset.Logger.LogDebug($"Configured to keep '{keyValuePair.Key}'. Skipping clear."); // Use new class name
                continue; // Skip to the next dictionary without clearing
            }

            // If we reach here, it means either:
            // 1. No config entry exists for this key, OR
            // 2. Config entry exists but is set to false (default behavior = Reset)
            NoUpgradeReset.Logger.LogDebug($"Resetting '{keyValuePair.Key}' (either not configured to keep, or explicitly set to reset)."); // Use new class name
            keyValuePair.Value.Clear(); // Clear the dictionary
        }
        // --- End selective clear ---

            // Original logic parts that should always run
            // Assuming field names are correct
            __instance.takenItemNames.Clear();
            __instance.runStats.Clear(); // runStats is separate and should likely always be cleared
            __instance.timePlayed = 0f;

            // IMPORTANT: Call the original RunStartStats method.
            // Since we are now handling the dictionary clearing selectively,
            // the original RunStartStats should be called to ensure proper initialization.
            // We are no longer completely replacing ResetAllStats logic, only modifying the loop part.
            __instance.RunStartStats();

            return false; // Still return false to prevent the *original* ResetAllStats loop from running
        }
    }

    // --- REMOVED StatsManager_RunStartStats_Patch ---
    // This patch is no longer needed as we are calling the original RunStartStats
    // from the modified ResetAllStats patch.


    // Patch to prevent save file deletion when leaving the game on the first level (levelsCompleted == 0)
    [HarmonyPatch(typeof(DataDirector), "SaveDeleteCheck")]
    public static class DataDirector_SaveDeleteCheck_Patch
    {
        // Prefix patch that runs before the original method.
        // Returning false prevents the original method from executing.
        public static bool Prefix(bool _leaveGame) // Match original parameters if needed, though they aren't used here
        {
            NoUpgradeReset.Logger.LogInfo("Skipping original DataDirector.SaveDeleteCheck method entirely.");
            return false; // Returning false skips the original method
        }
    }
}
