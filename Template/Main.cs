using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TurretAmmo
{
    public static class PluginInfo
    {
        public const string Name = "TurretAmmo";
        public const string Guid = "JPhix." + Name;
        public const string Version = "0.1";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> Turret_MaxAmmo;
        public void Awake()
        {
            Turret_MaxAmmo = Config.Bind("TurretAmmo", "Turret_MaxAmmo", 200, "Changes the max Ammo Turret can hold. (Valheim default is 40");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Turret), "HasAmmo")]
        public static void Turret_Ammo(ref int ___m_maxAmmo)
        {
            ___m_maxAmmo = Plugin.Turret_MaxAmmo.Value;
        }
    }

    [HarmonyPatch(typeof(Turret), "UpdateTarget")]
    public static class TurretUpdateTargetPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo targetMethod = typeof(TurretUpdateTargetPatch).GetMethod("ImFRIENDLYDAMMIT");
            return instructions.Select((CodeInstruction inst) => (!(inst.opcode == OpCodes.Call) || !(inst.operand is MethodInfo methodInfo) || !(methodInfo.Name == "FindClosestCreature")) ? inst : new CodeInstruction(OpCodes.Call, targetMethod));
        }

        public static Character ImFRIENDLYDAMMIT(Transform me, Vector3 eyePoint, float hearRange, float viewRange, float viewAngle, bool alerted, bool mistVision, bool passiveAggresive, bool includePlayers = true, bool includeTamed = true, List<Character> onlyTargets = null)
        {
            List<Character> allCharacters = Character.GetAllCharacters();
            Character character = null;
            float num = 99999f;
            foreach (Character item in allCharacters)
            {
                if ((!includePlayers && item is Player) || (!includeTamed && item.IsTamed()) || !AttackTarget(item))
                {
                    continue;
                }

                if (onlyTargets != null && onlyTargets.Count > 0)
                {
                    bool flag = false;
                    foreach (Character onlyTarget in onlyTargets)
                    {
                        if (item.m_name == onlyTarget.m_name)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        continue;
                    }
                }

                if (item.IsDead())
                {
                    continue;
                }

                BaseAI baseAI = item.GetBaseAI();
                if ((!(baseAI != null) || !baseAI.IsSleeping()) && BaseAI.CanSenseTarget(me, eyePoint, hearRange, viewRange, viewAngle, alerted, mistVision, item, passiveAggresive, isTamed: false))
                {
                    float num2 = Vector3.Distance(item.transform.position, me.position);
                    if ((double)num2 < (double)num || character == null)
                    {
                        character = item;
                        num = num2;
                    }
                }
            }

            return character;
        }

        internal static bool AttackTarget(Character target)
        {
            if (!target.m_nview.IsValid())
            {
                return true;
            }

            if (target.IsDead())
            {
                return false;
            }

            if (target.IsTamed())
            {
                return false;
            }

            if (target.GetComponents<Growup>().Any())
            {
                return false;
            }

            if (target.GetComponents<AnimalAI>().Any())
            {
                return false;
            }

            if (target.GetFaction() == Character.Faction.Players)
            {
                return false;
            }

            if (target.IsPVPEnabled())
            {
                return true;
            }

            if (!target.IsPlayer())
            {
                return true;
            }

            if (target.IsPlayer())
            {
                return false;
            }

            if (target == Player.m_localPlayer)
            {
                return false;
            }

            return true;
        }
    }
}