using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage.Game.Entity;

namespace avaness.BetterInventorySearch
{
    public static class InventoryController
    {
        private static FieldInfo searchBoxL, searchBoxR;
        private static FieldInfo ownersControlL, ownersControlR;

        public static void Patch(Harmony harmony)
        {
            Type inventoryController = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalInventoryController");

            MethodInfo original1 = inventoryController.GetMethod("SearchInList", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo postfix1 = typeof(InventoryController).GetMethod("SearchInList");
            harmony.Patch(original1, postfix: new HarmonyMethod(postfix1));

            MethodInfo original2 = inventoryController.GetMethod("ownerControl_InventoryContentsChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo postfix2 = typeof(InventoryController).GetMethod("ownerControl_InventoryContentsChanged");
            harmony.Patch(original2, postfix: new HarmonyMethod(postfix2));

            searchBoxL = AccessTools.Field(inventoryController, "m_searchBoxLeft");
            searchBoxR = AccessTools.Field(inventoryController, "m_searchBoxRight");
            ownersControlL = AccessTools.Field(inventoryController, "m_leftOwnersControl");
            ownersControlR = AccessTools.Field(inventoryController, "m_rightOwnersControl");
        }

        public static void ownerControl_InventoryContentsChanged(object __instance, MyGuiControlInventoryOwner control)
        {
            MyGuiControlList list = control.Owner as MyGuiControlList;
            if (list == null)
                return;

            MyGuiControlSearchBox searchBox;
            MyGuiControlList otherList = (MyGuiControlList)ownersControlL.GetValue(__instance);
            if (list == otherList)
            {
                searchBox = (MyGuiControlSearchBox)searchBoxL.GetValue(__instance);
            }
            else
            {
                otherList = (MyGuiControlList)ownersControlR.GetValue(__instance);
                if (list == otherList)
                    searchBox = (MyGuiControlSearchBox)searchBoxR.GetValue(__instance);
                else
                    return;
            }

            GetSearch(searchBox.TextBox, out bool emptyText, out string[] args);
            SearchInOwner(emptyText, args, control);
        }

        public static void SearchInList(MyGuiControlTextbox searchText, MyGuiControlList list)
        {
            GetSearch(searchText, out bool emptyText, out string[] args);
            foreach (MyGuiControlBase control in list.Controls)
            {
                if (!control.Visible)
                    continue;

                MyGuiControlInventoryOwner inventoryOwner = control as MyGuiControlInventoryOwner;
                if (inventoryOwner == null)
                    continue;
                SearchInOwner(emptyText, args, inventoryOwner);
            }
        }

        private static void GetSearch(MyGuiControlTextbox textbox, out bool emptyText, out string[] args)
        {
            StringBuilder sb = new StringBuilder();
            textbox.GetText(sb);
            string text = sb.ToString();
            emptyText = string.IsNullOrWhiteSpace(text);
            if (!emptyText)
                args = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            else
                args = null;
        }

        private static void SearchInOwner(bool emptyText, string[] args, MyGuiControlInventoryOwner inventoryOwner)
        {
            MyEntity e = inventoryOwner.InventoryOwner;
            bool noSearch = emptyText || e?.DisplayNameText == null || SearchMatches(args, e.DisplayNameText);

            foreach (MyGuiControlGrid grid in inventoryOwner.ContentGrids)
            {
                foreach (MyGuiGridItem gridItem in grid.Items)
                {
                    if (noSearch)
                        gridItem.Enabled = true;
                    else if (gridItem.UserData is MyPhysicalInventoryItem item && TryGetDisplayName(item, out string displayName))
                        gridItem.Enabled = SearchMatches(args, displayName);
                }
            }
        }

        private static bool TryGetDisplayName(MyPhysicalInventoryItem item, out string displayName)
        {
            MyPhysicalItemDefinition def = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Content);
            if (def == null || string.IsNullOrEmpty(def.DisplayNameText))
            {
                displayName = null;
                return false;
            }
            displayName = def.DisplayNameText;
            return true;
        }

        private static bool SearchMatches(string[] search, string item)
        {
            foreach (string arg in search)
            {
                if (!item.Contains(arg, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }
}
