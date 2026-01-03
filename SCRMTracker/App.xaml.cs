using System.IO;
using System.Windows;

namespace SCRMTracker
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check TrackerCore.dll exists
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TrackerCore.dll");
            
            if (!File.Exists(dllPath))
            {
                MessageBox.Show(
                    "TrackerCore.dll 파일을 찾을 수 없습니다.\n프로그램을 종료합니다.",
                    "오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Current.Shutdown();
                return;
            }

            // Initialize Settings.json
            Settings settings = Settings.GetInstance();
            settings.ChagneTheme(settings.MainThemeColor, settings.SubThemeColor);
        }
    }
}
