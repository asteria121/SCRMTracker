using ControlzEx.Theming;
using Newtonsoft.Json;
using SCRMTracker.Items;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SCRMTracker
{
    /// <summary>
    /// Class to manage program settings, synchronize with .\Settings.json
    /// </summary>
    public class Settings
    {
        [JsonProperty(Required = Required.Always)]
        public int MainThemeColor { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public int SubThemeColor { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string RecentAdpaterID { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool RememberAdapter { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool AutoCapture { get; set; }

        [JsonProperty(Required = Required.Always)]
        public bool BlackListWAV { get; set; }

        private static string SettingsJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.json");

        private static Settings? instance;

        /// <summary>
        /// Constructor of Settgins, contains default program settings
        /// </summary>
        private Settings()
        {
            MainThemeColor = 1;
            SubThemeColor = 0;
            RecentAdpaterID = "";
            RememberAdapter = true;
            AutoCapture = false;
            BlackListWAV = true;
        }

        /// <summary>
        /// Create default Settings class object
        /// </summary>
        /// <returns>Default Settings class object</returns>
        private static Settings CreateDefaultSettingsJson()
        {
            Settings defaultSettings = new Settings();
            string json = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
            File.WriteAllText(SettingsJsonPath, json);
            return defaultSettings;
        }

        /// <summary>
        /// Singleton method of Settings class
        /// </summary>
        /// <returns>Settings class object</returns>
        public static Settings GetInstance()
        {
            try
            {
                if (instance == null)
                {
                    // Create new settings file if not exists
                    if (!File.Exists(SettingsJsonPath))
                    {
                        instance = CreateDefaultSettingsJson();
                        return instance;
                    }

                    // Create new default settings file if file does not contain data
                    string json = File.ReadAllText(SettingsJsonPath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        instance = CreateDefaultSettingsJson();
                        return instance;
                    }

                    // Create new default settings file if serialize failed
                    var settings = JsonConvert.DeserializeObject<Settings>(json);
                    if (settings == null)
                    {
                        instance = CreateDefaultSettingsJson();
                        return instance;
                    }

                    instance = settings;
                }

                return instance;
            }
            catch (JsonReaderException ex)
            {
                // Create new settings file if json read failed
                MessageBox.Show($"설정 로딩 중 다음 오류가 발생했습니다.\n설정 파일을 초기화합니다.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (JsonSerializationException ex)
            {
                // Create new settings file if serialize failed
                MessageBox.Show($"설정 로딩 중 다음 오류가 발생했습니다.\n설정 파일을 초기화합니다.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 로딩 중 다음 오류가 발생했습니다.\n프로그램을 종료합니다.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            instance = CreateDefaultSettingsJson();
            return instance;
        }

        /// <summary>
        /// Change theme color for program
        /// </summary>
        /// <param name="mainThemeColorIdx">ThemeColor class's mainColor List index</param>
        /// <param name="subThemeColorIdx">ThemeColor class's subColor List index</param>
        public void ChagneTheme(int mainThemeColorIdx, int subThemeColorIdx)
        {
            string mainThemeColorStr;
            string subThemeColorStr;

            MainThemeColor = mainThemeColorIdx;
            SubThemeColor = subThemeColorIdx;

            if (subThemeColorIdx == 0)
            {
                // Get system default theme (Light/Dark)
                subThemeColorStr = ThemeColor.GetSystemThemeColorStr();
            }
            else
            {
                // User selected subcolor
                subThemeColorStr = ThemeColor.GetSubColors()[SubThemeColor].Name;
            }

            mainThemeColorStr = ThemeColor.GetMainColors()[MainThemeColor].Name;
            ThemeManager.Current.ChangeTheme(Application.Current, $"{subThemeColorStr}.{mainThemeColorStr}");
        }

        /// <summary>
        /// Synchronize Settings class object with .\Settings.json file
        /// </summary>
        public void UpdateSettingsJson()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsJsonPath, json);
            }
            catch { }
        }
    }
}
