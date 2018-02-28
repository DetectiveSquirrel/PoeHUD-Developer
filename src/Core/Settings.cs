using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;

namespace DeveloperTool.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            ShowWindow = true;
            DebugNearestEnts = Keys.NumPad6;
            NearestEntsRange = new RangeNode<int>(300, 1, 2000);
        }
        
        [Menu("Show Developer Information")]
        public ToggleNode ShowWindow { get; set; }

        [Menu("Debug Nearest Ents")]
        public HotkeyNode DebugNearestEnts { get; set; }

        [Menu("Ents Debug Range")]
        public RangeNode<int> NearestEntsRange { get; set; }
    }
}