using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Developer_Tool
{
    public class DevSettings : SettingsBase
    {
        public DevSettings()
        {
            ShowWindow = true;
        }
        
        [Menu("Show Dev Window")]
        public ToggleNode ShowWindow { get; set; }
    }
}