using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace DeveloperTool.Core
{
    public class DevSettings : SettingsBase
    {
        public DevSettings()
        {
            ShowWindow = true;
        }
        
        [Menu("Show Developer Information")]
        public ToggleNode ShowWindow { get; set; }
    }
}