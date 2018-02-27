using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace DeveloperTool.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            ShowWindow = true;
        }
        
        [Menu("Show Developer Information")]
        public ToggleNode ShowWindow { get; set; }
    }
}