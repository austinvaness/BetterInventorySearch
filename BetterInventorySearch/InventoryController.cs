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
        public static void Patch(Harmony harmony)
        {
            Type inventoryController = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalInventoryController");

            MethodInfo original1 = inventoryController.GetMethod("SearchInList", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo postfix1 = typeof(InventoryController).GetMethod(nameof(SearchInList));
            harmony.Patch(original1, postfix: new HarmonyMethod(postfix1));

            MethodInfo original2 = inventoryController.GetMethod("ownerControl_InventoryContentsChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo postfix2 = typeof(InventoryController).GetMethod(nameof(ownerControl_InventoryContentsChanged));
            harmony.Patch(original2, postfix: new HarmonyMethod(postfix2));


        }

        public static void ownerControl_InventoryContentsChanged(MyGuiControlInventoryOwner control)
        {
            if(TryGetActiveSearchBox(control, out MyGuiControlSearchBox searchBox))
            {
                GetSearch(searchBox.TextBox, out bool emptyText, out string[] args);
                SearchInOwner(emptyText, args, control);
            }
        }

        private static bool TryGetActiveSearchBox(MyGuiControlInventoryOwner inventoryOwner, out MyGuiControlSearchBox searchBox)
        {
            searchBox = null;

            // Get the parent list and the parent tab
            MyGuiControlList list = inventoryOwner.Owner as MyGuiControlList;
            if (list == null)
                return false;

            MyGuiControlTabPage parent = list.Owner as MyGuiControlTabPage;
            if (parent == null)
                return false;

            // Get the search bar
            // Reference: MyTerminalInventoryController.Init()
            MyGuiControlList leftList = parent.Controls.GetControlByName("LeftInventory") as MyGuiControlList;
            if (leftList == null)
                return false;
            if (list == leftList)
            {
                searchBox = parent.Controls.GetControlByName("BlockSearchLeft") as MyGuiControlSearchBox;
                return searchBox != null;
            }

            MyGuiControlList rightList = parent.Controls.GetControlByName("RightInventory") as MyGuiControlList;
            if (rightList == null)
                return false;
            if (list == rightList)
            {
                searchBox = parent.Controls.GetControlByName("BlockSearchRight") as MyGuiControlSearchBox;
                return searchBox != null;
            }

            return false;
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
                        SetEnabled(gridItem, true);
                    else if (gridItem.UserData is MyPhysicalInventoryItem item && TryGetDisplayName(item, out string displayName))
                        SetEnabled(gridItem, SearchMatches(args, displayName));
                }
            }
        }

        private static void SetEnabled(MyGuiGridItem item, bool enabled)
        {
            if (enabled)
            {
                item.MainIconColorMask = VRageMath.Vector4.One;
                item.Enabled = true;
            }
            else
            {
                item.MainIconColorMask = new VRageMath.Vector4(1, 1, 1, 0.5f);
                item.Enabled = false;
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
