using Microsoft.Win32;
using System.Windows.Media;

namespace SCRMTracker.Items
{
    public class ThemeColor
    {
        public string Name { get; private set; }

        public Color Color { get; private set; }

        /// <summary>
        /// Constructor of ThemeColor, contains program UI color information
        /// </summary>
        /// <param name="name"></param>
        /// <param name="color"></param>
        private ThemeColor(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        private static List<ThemeColor>? mainColors = null;

        private static List<ThemeColor>? subColors = null;

        /// <summary>
        /// Get main theme color list
        /// </summary>
        /// <returns>23 main colors</returns>
        public static List<ThemeColor> GetMainColors()
        {
            mainColors = new List<ThemeColor>
            {
                new ThemeColor("Amber", (Color)ColorConverter.ConvertFromString("#FFC7890F")),
                new ThemeColor("Blue", (Color)ColorConverter.ConvertFromString("#FF0767B3")),
                new ThemeColor("Brown", (Color)ColorConverter.ConvertFromString("#FF825A2C")),
                new ThemeColor("Cobalt", (Color)ColorConverter.ConvertFromString("#FF0050EF")),
                new ThemeColor("Crimson", (Color)ColorConverter.ConvertFromString("#FFA20025")),
                new ThemeColor("Cyan", (Color)ColorConverter.ConvertFromString("#FF1BA1E2")),
                new ThemeColor("Emerald", (Color)ColorConverter.ConvertFromString("#FF008A00")),
                new ThemeColor("Green", (Color)ColorConverter.ConvertFromString("#FF60A917")),
                new ThemeColor("Indigo", (Color)ColorConverter.ConvertFromString("#FF6A00FF")),
                new ThemeColor("Lime", (Color)ColorConverter.ConvertFromString("#FFA4C400")),
                new ThemeColor("Magenta", (Color)ColorConverter.ConvertFromString("#FFD80073")),
                new ThemeColor("Mauve", (Color)ColorConverter.ConvertFromString("#FF76608A")),
                new ThemeColor("Olive", (Color)ColorConverter.ConvertFromString("#FF6D8764")),
                new ThemeColor("Orange", (Color)ColorConverter.ConvertFromString("#FFFA6800")),
                new ThemeColor("Pink", (Color)ColorConverter.ConvertFromString("#FFF472D0")),
                new ThemeColor("Purple", (Color)ColorConverter.ConvertFromString("#FF574EB9")),
                new ThemeColor("Red", (Color)ColorConverter.ConvertFromString("#FFE51400")),
                new ThemeColor("Sienna", (Color)ColorConverter.ConvertFromString("#FFA0522D")),
                new ThemeColor("Steel", (Color)ColorConverter.ConvertFromString("#FF647687")),
                new ThemeColor("Taupe", (Color)ColorConverter.ConvertFromString("#FF87794E")),
                new ThemeColor("Teal", (Color)ColorConverter.ConvertFromString("#FF00ABA9")),
                new ThemeColor("Violet", (Color)ColorConverter.ConvertFromString("#FFAA00FF")),
                new ThemeColor("Yellow", (Color)ColorConverter.ConvertFromString("#FFE3C800"))
            };

            return mainColors;
        }

        /// <summary>
        /// Get sub theme color list
        /// </summary>
        /// <returns>3 sub colors</returns>
        public static List<ThemeColor> GetSubColors()
        {
            if (subColors == null)
            {
                string systemTheme = GetSystemThemeColorStr();
                string systemThemeStr = $"Follow System ({systemTheme})";
                Color systemColor;
                if (systemTheme == "Dark")
                {
                    systemColor = Colors.Black;
                }
                else
                {
                    systemColor = Colors.White;
                }

                subColors = new List<ThemeColor>
                {
                    new ThemeColor(systemThemeStr, systemColor),
                    new ThemeColor("Light", Colors.White),
                    new ThemeColor("Dark", Colors.Black)
                };
            }

            return subColors;
        }

        /// <summary>
        /// Get current system's theme (light/dark) by reading registry
        /// </summary>
        /// <returns>String "Light" or "Dark"</returns>
        public static string GetSystemThemeColorStr()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            // 0 = DarkMode, 1 = LightMode
                            if ((int)value == 0)
                            {
                                return "Dark";
                            }
                            else
                            {
                                return "Light";
                            }
                        }
                    }
                }
            }
            catch { }

            return "Light";
        }
    }
}
