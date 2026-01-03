using System.IO;
using System.Windows.Media;

namespace SCRMTracker.Sounds
{
    public class WAVPlayer
    {
        private MediaPlayer Player = new MediaPlayer();

        private static WAVPlayer? instance = null;

        /// <summary>
        /// Singleton method of WAVPlayer class
        /// </summary>
        /// <returns>WAVPlayer object</returns>
        public static WAVPlayer GetInstance()
        {
            if (instance == null)
                instance = new WAVPlayer();

            return instance;
        }

        /// <summary>
        /// Play WAV file
        /// </summary>
        /// <param name="fileName">WAV file name which located in .\Sounds folder</param>
        public void PlayWAVFile(string fileName)
        {
            if (Settings.GetInstance().BlackListWAV)
            {
                string wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", fileName);
                Player.Open(new Uri(wavPath, UriKind.Absolute));
                Player.Volume = 0.3;
                Player.Play();
            }
        }
    }
}
