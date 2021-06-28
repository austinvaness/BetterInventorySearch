using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using System;
using System.Reflection;

namespace avaness.BetterInventorySearch
{
    public static class ProductionController
    {
        private static FieldInfo searchBox;

        public static void Patch(Harmony harmony)
        {
            Type productionController = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalProductionController");

            MethodInfo original = productionController.GetMethod("SelectAndShowAssembler", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo prefix = typeof(ProductionController).GetMethod("Prefix");
            MethodInfo postfix = typeof(ProductionController).GetMethod("Postfix");
            harmony.Patch(original, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

            searchBox = AccessTools.Field(productionController, "m_blueprintsSearchBox");
        }

        public static bool Prefix(object __instance, out string __state)
        {
            MyGuiControlSearchBox searchBox = (MyGuiControlSearchBox)ProductionController.searchBox.GetValue(__instance);
            __state = searchBox.SearchText;
            return true;
        }

        public static void Postfix(object __instance, string __state)
        {
            MyGuiControlSearchBox searchBox = (MyGuiControlSearchBox)ProductionController.searchBox.GetValue(__instance);
            searchBox.SearchText = __state;
        }
    }
}
