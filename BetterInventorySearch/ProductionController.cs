using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Reflection;

namespace avaness.BetterInventorySearch
{
    public static class ProductionController
    {
        private static FieldInfo searchBox;
        private static FieldInfo combobox;

        public static void Patch(Harmony harmony)
        {
            Type productionController = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalProductionController");

            MethodInfo original = productionController.GetMethod("SelectAndShowAssembler", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo prefix = typeof(ProductionController).GetMethod("PrefixSearch");
            MethodInfo postfix = typeof(ProductionController).GetMethod("PostfixSearch");
            harmony.Patch(original, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

            original = productionController.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            postfix = typeof(ProductionController).GetMethod("SortBlocks");
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            original = productionController.GetMethod("TerminalSystem_BlockAdded", BindingFlags.Instance | BindingFlags.NonPublic);
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            searchBox = AccessTools.Field(productionController, "m_blueprintsSearchBox");
            combobox = AccessTools.Field(productionController, "m_comboboxAssemblers");
        }

        public static bool PrefixSearch(object __instance, out string __state)
        {
            MyGuiControlSearchBox searchBox = (MyGuiControlSearchBox)ProductionController.searchBox.GetValue(__instance);
            __state = searchBox.SearchText;
            return true;
        }

        public static void PostfixSearch(object __instance, string __state)
        {
            MyGuiControlSearchBox searchBox = (MyGuiControlSearchBox)ProductionController.searchBox.GetValue(__instance);
            searchBox.SearchText = __state;
        }

        public static void SortBlocks(object __instance)
        {
            MyGuiControlCombobox combobox = (MyGuiControlCombobox)ProductionController.combobox.GetValue(__instance);
            combobox?.SortItemsByValueText();
        }
    }
}
