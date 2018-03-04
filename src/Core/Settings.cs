using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;
using ImGuiVector2 = System.Numerics.Vector2;

namespace DeveloperTool.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            DebugNearestEnts = Keys.NumPad6;
            DebugHoverItem = Keys.NumPad5;
            NearestEntsRange = new RangeNode<int>(300, 1, 2000);
            var centerPos = BasePlugin.API.GameController.Window.GetWindowRectangle().Center;
            LastSettingSize = new ImGuiVector2(620, 376);
            LastSettingPos = new ImGuiVector2(centerPos.X - LastSettingSize.X / 2, centerPos.Y - LastSettingSize.Y / 2);
            LimitEntriesDrawn = true;
            EntriesDrawLimit = new RangeNode<int>(500, 1, 5000);
        }
        
        public HotkeyNode DebugNearestEnts { get; set; }
        public HotkeyNode DebugHoverItem { get; set; }
        public RangeNode<int> NearestEntsRange { get; set; }

        public ImGuiVector2 LastSettingPos { get; set; }
        public ImGuiVector2 LastSettingSize { get; set; }

        public ToggleNode LimitEntriesDrawn { get; set; }
        public RangeNode<int> EntriesDrawLimit { get; set; }
    }
}