using HarmonyLib;
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
            InventoryController.Patch(harmony);
            ProductionController.Patch(harmony);
        }

        public void Update()
        { }
    }
}
