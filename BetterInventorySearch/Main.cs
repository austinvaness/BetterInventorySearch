using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VRage.Game.Entity;
using VRage.Plugins;

namespace avaness.BetterInventorySearch
{
    public class Main : IPlugin
    {
        public void Dispose()
        { }

        public void Init(object gameInstance)
        {
            Harmony harmony = new Harmony("avaness.BetterInventorySearch");

            Type originalType = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalInventoryController");
            MethodInfo original1 = originalType.GetMethod("SearchInList", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo postfix1 = typeof(Main).GetMethod("Postfix");
            harmony.Patch(original1, postfix: new HarmonyMethod(postfix1));
        }

        public void Update()
        { }

        public static void Postfix(MyGuiControlTextbox searchText, MyGuiControlList list)
        {
            StringBuilder sb = new StringBuilder();
            searchText.GetText(sb);
            string text = sb.ToString();
            bool emptyText = string.IsNullOrWhiteSpace(text);
            string[] args = null;
            if(!emptyText)
                args = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (MyGuiControlBase control in list.Controls)
            {
                if (!control.Visible)
                    continue;

                MyGuiControlInventoryOwner inventoryOwner = control as MyGuiControlInventoryOwner;
                if (inventoryOwner == null)
                    continue;
                
                MyEntity e = inventoryOwner.InventoryOwner;
                bool noSearch = emptyText || e?.DisplayNameText == null || SearchMatches(args, e.DisplayNameText);

                foreach (MyGuiControlGrid grid in inventoryOwner.ContentGrids)
                {
                    foreach (MyGuiGridItem gridItem in grid.Items)
                    {
                        if(noSearch)
                        {
                            gridItem.OverlayPercent = 0;
                        }
                        else if (gridItem.UserData is MyPhysicalInventoryItem item && TryGetDisplayName(item, out string displayName))
                        {
                            if (SearchMatches(args, displayName))
                                gridItem.OverlayPercent = 0;
                            else
                                gridItem.OverlayPercent = 1;
                        }
                    }
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
                if (item.Contains(arg, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
