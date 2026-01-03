using System.Windows;

namespace SCRMTracker
{
    /// <summary>
    /// Display player database modify flyout interface
    /// </summary>
    public partial class ModifyFlyout
    {
        // Flag value to check database is modified at flyout
        private bool _SaveResult = false;
        public bool SaveResult
        {
            get {  return _SaveResult; }
            set {  _SaveResult = value; }
        }

        public ModifyFlyout()
        {
            InitializeComponent();
        }

        private void confirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsOpen = false;
            _SaveResult = true;
        }
    }
}
