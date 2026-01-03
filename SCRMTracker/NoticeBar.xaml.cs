using System.Windows.Media;

namespace SCRMTracker
{
    /// <summary>
    /// Display customized notice bar
    /// </summary>
    public partial class NoticeBar
    {
        public NoticeBar()
        {
            InitializeComponent();
        }

        public NoticeBar(string _message, Brush brush, MahApps.Metro.Controls.Position pos)
        {
            InitializeComponent();
            NotificationMessageBlock.Text = _message;
            Background = brush;
            Position = pos;
        }
    }
}
