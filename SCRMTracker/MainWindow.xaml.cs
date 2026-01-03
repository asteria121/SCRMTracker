using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Data.Sqlite;
using SCRMTracker.Items;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SCRMTracker
{
    public partial class MainWindow : MetroWindow
    {
        // Item binding source for network adapters ComboBox
        private ObservableCollection<NetworkAdapterInfo> NetworkAdapters = new ObservableCollection<NetworkAdapterInfo>();

        // Item binding source for joined player DataGrid
        private ObservableCollection<JoinedPlayer> JoinedPlayersCollection = new ObservableCollection<JoinedPlayer>();

        // Item binding source for DatabasePlayer DataGrid
        private DatabasePlayerRepo DbPlayerRepo;

        // DataGrid filter
        private CollectionView? _view;

        // Mutual exclusion for ObservableCollection<JoinedPlayer>
        private readonly object JoinedPlayersDatagridLock = new object();

        // Mutual exclusion for DatabasePlayerRepo
        private readonly object DatabasePlayerDatagridLock = new object();

        // GC-prevented callback delegate
        private PlayerJoinCallback PlayerJoinDelegate;

        // Global MetroWindow dialog settings
        private readonly MetroDialogSettings GlobalDialogSettings;

        public MainWindow()
        {
            InitializeComponent();

            // Global MetroWindow dialog settings
            GlobalDialogSettings = new MetroDialogSettings();
            GlobalDialogSettings.AnimateShow = false;
            GlobalDialogSettings.AnimateHide = false;

            // DatabasePlayer manager class
            DbPlayerRepo = new DatabasePlayerRepo();

            // Do item binding
            BindingOperations.EnableCollectionSynchronization(JoinedPlayersCollection, JoinedPlayersDatagridLock);
            BindingOperations.EnableCollectionSynchronization(DbPlayerRepo.DatabasePlayersCollection, DatabasePlayerDatagridLock);

            // Set delegate function pointer which is not freed by GC
            PlayerJoinDelegate = PlayerJoinNotifyRoutine;
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow wnd = new SettingsWindow();
            wnd.ShowDialog();
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            MetroWindow wnd = new AboutWindow();
            wnd.ShowDialog();
        }

        /// <summary>
        /// Event to implement autoscroll when player joined
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void JoinedPlayersCollection_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (autoScrollCheckBox?.IsChecked == true && e.NewItems != null && e.NewItems.Count > 0)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        currentRoomDataGrid.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
                    }), DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Display notice bar at bottom or top, implemented with flyout
        /// </summary>
        /// <param name="message">Message of notice bar</param>
        /// <param name="color">Color of notice bar</param>
        /// <param name="pos">Notice bar display position</param>
        private void ShowNoticeBar(string message, Brush color, Position pos = Position.Top)
        {
            var flyout = new NoticeBar(message, color, pos);
            flyout.AutoCloseInterval = 3000;
            flyout.IsAutoCloseEnabled = true;

            RoutedEventHandler? closingFinishedHandler = null;
            closingFinishedHandler = (o, args) =>
            {
                flyout.ClosingFinished -= closingFinishedHandler;
                ((MainWindow)Application.Current.MainWindow).flyoutsControl.Items.Remove(flyout);
            };

            flyout.ClosingFinished += closingFinishedHandler;
            ((MainWindow)Application.Current.MainWindow).flyoutsControl.Items.Add(flyout);
            flyout.IsOpen = true;
        }

        /// <summary>
        /// Display log message at logTextBox with UI thread dispatcher
        /// </summary>
        /// <param name="text">Log message</param>
        private void DispatchLogMessage(string text)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                logTextBox.Text += text;
                logTextBox.Text += Environment.NewLine;
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// Event for search text box auto focus (Ctrl+F)
        /// </summary>
        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                searchTextBox.Focus();
                searchTextBox.SelectAll();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Window load event, load database, item bind, set datagrid filter, and load network adapters
        /// </summary>
        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var progressDialog = await this.ShowProgressAsync("Loading Database", "플레이어 데이터베이스 테이블 초기화 중입니다. 잠시만 기다려주세요.", settings: GlobalDialogSettings);
            progressDialog.SetIndeterminate();

            try
            {
                // Connect to database
                await Database.GetInstance().InitializeTableAsync();
                progressDialog.SetMessage("플레이어 데이터베이스 로딩중입니다. 잠시만 기다려주세요.");
                await DbPlayerRepo.LoadFromDatabaseAsync();
                playerDbDataGrid.ItemsSource = DbPlayerRepo.DatabasePlayersCollection;

                // Filter collection view after setting itemsource
                _view = (CollectionView)CollectionViewSource.GetDefaultView(playerDbDataGrid.ItemsSource);
                _view.Filter = DatagridFilter;
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("ERROR", $"데이터베이스 로딩 중 오류가 발생했습니다.\n{ex.Message}", settings: GlobalDialogSettings);
            }
            finally
            {
                if (progressDialog != null)
                    await progressDialog.CloseAsync();
            }

            // Check for network adapter
            JoinedPlayersCollection.CollectionChanged += JoinedPlayersCollection_CollectionChanged;
            currentRoomDataGrid.ItemsSource = JoinedPlayersCollection;
            LoadNetworkAdapters();
        }

        /// <summary>
        /// Load network adapter list to combobox, auto select adapter & start capture if possible
        /// </summary>
        private async void LoadNetworkAdapters()
        {
            try
            {
                // Get all enabled network adapters
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ToList();

                if (interfaces.Count == 0)
                {
                    await this.ShowMessageAsync("ERROR", "사용 가능한 네트워크 어댑터를 찾을 수 없습니다.", settings: GlobalDialogSettings);
                    Application.Current.Shutdown();
                }

                foreach (var networkInterface in interfaces)
                {
                    var adapterInfo = new NetworkAdapterInfo(networkInterface.Id, networkInterface.Name, networkInterface.Description, $"{networkInterface.Name} - {networkInterface.Description}");
                    NetworkAdapters.Add(adapterInfo);
                }

                adapterComboBox.ItemsSource = NetworkAdapters;

                if (NetworkAdapters.Count > 0)
                {
                    adapterComboBox.SelectedIndex = 0;
                    Settings settings = Settings.GetInstance();
                    // Auto select if recent adapter option is enabled, auto start capture if option enabled
                    if (settings.RememberAdapter && !string.IsNullOrWhiteSpace(Settings.GetInstance().RecentAdpaterID))
                    {
                        foreach (var adapter in NetworkAdapters)
                        {
                            if (adapter.Id == settings.RecentAdpaterID)
                            {
                                adapterComboBox.SelectedItem = adapter;
                                if (settings.AutoCapture) await StartCapture(adapter.Id, PlayerJoinDelegate);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Auto select general ethernet adapter
                        foreach (var adapter in NetworkAdapters)
                        {
                            if (adapter.FriendlyName.Contains("Realtek") || adapter.FriendlyName.Contains("Intel"))
                            {
                                adapterComboBox.SelectedItem = adapter;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("ERROR", $"네트워크 어댑터 목록 로딩 중 오류가 발생했습니다.\n{ex.Message}", settings: GlobalDialogSettings);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Helper method for start packet capture (auto capture, click button)
        /// </summary>
        /// <param name="adapterId">ID of network adapter</param>
        /// <param name="callbackFunction">Callback function which is triggered by player join</param>
        /// <returns></returns>
        private async Task StartCapture(string adapterId, PlayerJoinCallback callbackFunction)
        {
            if (string.IsNullOrWhiteSpace(adapterId))
            {
                ShowNoticeBar("네트워크 어댑터를 선택 해야합니다", Brushes.Orange, Position.Bottom);
                return;
            }

            try
            {
                bool result = false;
                startCaptureButton.IsEnabled = false;
                await Task.Run(() => result = NativeMethods.InitializeCaptureService(adapterId, callbackFunction));
                if (result == false)
                {
                    startCaptureButton.IsEnabled = true;
                    await this.ShowMessageAsync("ERROR", $"다음 오류가 발생해 패킷 캡처를 시작하지 못했습니다.\n{NativeMethods.GetLastExceptionString()}", settings: GlobalDialogSettings);
                }
                else
                {
                    stopCaptureButton.IsEnabled = true;
                    ShowNoticeBar("패킷 캡처를 시작했습니다.", Brushes.Green, Position.Bottom);
                    logTextBox.Text += "패킷 캡처를 시작했습니다.\n";
                }
            }
            catch (Exception ex)
            {
                startCaptureButton.IsEnabled = true;
                await this.ShowMessageAsync("ERROR", $"다음 오류가 발생해 패킷 캡처를 시작하지 못했습니다.\n{ex.Message}", settings: GlobalDialogSettings);
            }
        }

        private async void startCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            await StartCapture(((NetworkAdapterInfo)adapterComboBox.SelectedItem).Id, PlayerJoinDelegate);

            NetworkAdapterInfo adapter = (NetworkAdapterInfo)adapterComboBox.SelectedItem;
            Settings.GetInstance().RecentAdpaterID = adapter.Id;
            Settings.GetInstance().UpdateSettingsJson();
        }

        private async void stopCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            stopCaptureButton.IsEnabled = false;
            await Task.Run(() => NativeMethods.ReleaseCaptureService());
            adapterComboBox.IsEnabled = true;
            startCaptureButton.IsEnabled = true;
            ShowNoticeBar("패킷 캡처를 중지했습니다.", Brushes.Green, Position.Bottom);
            logTextBox.Text += "패킷 캡처를 중지했습니다.\n";
        }

        private static string MarshalUtf8String(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            int length = 0;
            while (Marshal.ReadByte(ptr, length) != 0)
            {
                length++;
                if (length > 1024) break;
            }

            if (length == 0)
                return string.Empty;

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Player join event callback function from C++ TrackerCore.dll
        /// </summary>
        /// <param name="infoPtr"></param>
        public void PlayerJoinNotifyRoutine(IntPtr infoPtr)
        {
            try
            {
                // Marshall ptr to proper data type
                if (infoPtr == IntPtr.Zero)
                    return;

                PlayerInfo info = Marshal.PtrToStructure<PlayerInfo>(infoPtr);
                string battleTag = MarshalUtf8String(info.BattleTag);
                string userName = MarshalUtf8String(info.UserName);

                // Mask & Hash IP address if not blizzard proxy server ip
                var ipWrapper = new IPWrapper(info.IPAddress);
                string maskedIPAddress = ipWrapper.ToMaskedIP();
                byte[]? hashedIPAddress = ipWrapper.ToHashBytes();

                // Use dictionary to search in O(1), dictionary is managed by CollectionChanged event
                var dbPlayer = DbPlayerRepo.FindPlayerByBattleTag(battleTag);
                if (dbPlayer == null)
                {
                    try
                    {
                        // New player joined (not registered in database), add to database
                        var databasePlayer = new DatabasePlayer(battleTag, userName, hashedIPAddress);
                        DispatchLogMessage($"신규 플레이어 추가: {battleTag}");
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            lock (DatabasePlayerDatagridLock)
                            {
                                DbPlayerRepo.DatabasePlayersCollection.Add(databasePlayer);
                            }
                        }), DispatcherPriority.Background);
                    }
                    catch (SqliteException ex)
                    {
                        DispatchLogMessage($"신규 플레이어 등록 중 오류 발생: {ex.Message}");
                    }
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (DatabasePlayerDatagridLock)
                        {
                            dbPlayer.Update(_userName: userName, _hashedIPAddress: hashedIPAddress);
                        }
                    }), DispatcherPriority.Background);

                    // Database registered player joined
                    if (dbPlayer.IsBlackList == true)
                    {
                        // Play WAV file if blacklist player joined (option)
                        SCRMTracker.Sounds.WAVPlayer.GetInstance().PlayWAVFile("blacklist.wav");
                        DispatchLogMessage($"블랙리스트 등록 플레이어 입장: {battleTag}");
                    }
                }

                // Check for same IP players are exist
                var sameIPPlayerList = DbPlayerRepo.FindPlayersByIPHash(hashedIPAddress);
                if (sameIPPlayerList != null)
                {
                    bool display = false;
                    string sameIPPlayerLogMsg = $"{battleTag}와 동일 IP를 사용하는 플레이어 확인: ";
                    foreach (var ipPlayer in sameIPPlayerList)
                    {
                        if (ipPlayer.BattleTag != battleTag)
                        {
                            display = true;
                            if (ipPlayer.IsBlackList == true)
                            {
                                sameIPPlayerLogMsg += $" {ipPlayer.BattleTag}(블랙리스트)";
                            }
                            else
                            {
                                sameIPPlayerLogMsg += $" {ipPlayer.BattleTag}";
                            }
                        }
                    }

                    if (display == true)
                        DispatchLogMessage(sameIPPlayerLogMsg);
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    lock (JoinedPlayersDatagridLock)
                    {
                        JoinedPlayersCollection.Add(new JoinedPlayer(info.Pid, userName, battleTag, maskedIPAddress, dbPlayer?.IsBlackList, dbPlayer?.Comment));
                    }
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("=====================================\n");
                sb.Append($"콜백 핸들러 오류 발생\n{ex.Message}\r\n{ex.StackTrace}\n");
                sb.Append("=====================================");
                DispatchLogMessage(sb.ToString());
            }
        }

        /// <summary>
        /// Helper method for DataGrid double click copy interaction
        /// </summary>
        /// <returns>DataGridCell object</returns>
        private DataGridCell? GetDataGridCellFromMouseEvent(MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;

            while (source != null && !(source is DataGridCell))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as DataGridCell;
        }

        /// <summary>
        /// Helper method for DataGrid double click copy interaction
        /// </summary>
        /// <returns>DataGridCell's text</returns>
        private string GetCellContent(DataGridCell cell)
        {
            if (cell.Content is TextBlock textBlock)
            {
                return textBlock.Text;
            }

            return cell.Content?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// DataGrid cell double click event to implement copy interaction
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyCellContentByDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var cell = GetDataGridCellFromMouseEvent(e);
            if (cell != null)
            {
                var content = GetCellContent(cell);
                if (!string.IsNullOrEmpty(content))
                {
                    Clipboard.SetText(content);
                    ShowNoticeBar($"클립보드 복사 완료: {content}", color: Brushes.Green, pos: Position.Bottom);
                }
            }
        }

        private void clearRoomListButton_Click(object sender, RoutedEventArgs e)
        {
            lock (JoinedPlayersDatagridLock)
            {
                JoinedPlayersCollection.Clear();
            }
        }

        private void searchOptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private void showBlacklistCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _view?.Refresh();
        }

        /// <summary>
        /// DataGrid filter method for DatabasePlayer DataGrid search
        /// </summary>
        /// <param name="item">DatabasePlayer object</param>
        /// <returns>Item filtering result (ex. true means not filtered)</returns>
        private bool DatagridFilter(object item)
        {
            bool result = true;
            string searchOption = searchOptionComboBox.Text;
            string searchText = searchTextBox.Text.ToLower();
            DatabasePlayer player = (DatabasePlayer)item;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                if (searchOption == "배틀태그")
                {
                    result &= player.BattleTag.ToLower().Contains(searchText);
                }
                else if (searchOption == "ID")
                {
                    result &= player.UsernameList.Any(s => s.ToLower().Contains(searchText)); ;
                }
                else if (searchOption == "메모")
                {
                    result &= player.Comment.ToLower().Contains(searchText);
                }
            }

            if (showBlacklistCheckBox.IsChecked == true)
            {
                result &= player.IsBlackList;
            }

            return result;
        }

        private void modifyPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (playerDbDataGrid.SelectedItems.Count != 1)
            {
                ShowNoticeBar("데이터베이스에서 1개의 플레이어를 선택하세요.", Brushes.Orange);
                return;
            }

            var flyout = new ModifyFlyout();
            DatabasePlayer player = (DatabasePlayer)playerDbDataGrid.SelectedItem;
            flyout.BattleTagTextBox.Text = player.BattleTag;
            flyout.MeetDateTimePicker.SelectedDate = DateTime.Parse(player.MeetDate);
            flyout.CommentTextBox.Text = player.Comment;
            flyout.IsBlackListCheckBox.IsChecked = player.IsBlackList;
            RoutedEventHandler? closingFinishedHandler = null;
            closingFinishedHandler = async (o, args) =>
            {
                if (flyout.SaveResult == true)
                {
                    try
                    {
                        bool isBlackList = flyout.IsBlackListCheckBox.IsChecked != null && flyout.IsBlackListCheckBox.IsChecked.Value;
                        string comment = flyout.CommentTextBox.Text;
                        
                        lock (DatabasePlayerDatagridLock)
                        {
                            player.Update(_isBlackList: isBlackList, _comment: comment); // Trigger ObservableCollection update to refresh UI
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.ShowMessageAsync("ERROR", $"플레이어 정보 수정 중 오류가 발생했습니다.\n{ex.Message}", settings: GlobalDialogSettings);
                    }
                }

                flyout.ClosingFinished -= closingFinishedHandler;
                flyoutsControl.Items.Remove(flyout);
            };
            flyout.ClosingFinished += closingFinishedHandler;
            flyoutsControl.Items.Add(flyout);
            flyout.IsOpen = true;
        }

        private async void removePlayerButton_Click(object sender, RoutedEventArgs e)
        {
            if (playerDbDataGrid.SelectedItems.Count < 1)
            {
                ShowNoticeBar("데이터베이스에서 최소 1개의 플레이어를 선택하세요.", Brushes.Orange);
                return;
            }

            var result = await this.ShowMessageAsync("ARE YOU SURE??", $"{playerDbDataGrid.SelectedItems.Count}개의 플레이어 정보를 삭제 하시겠습니까?\n이 작업은 되돌릴 수 없습니다."
                , MessageDialogStyle.AffirmativeAndNegative, settings: GlobalDialogSettings);
            if (result == MessageDialogResult.Negative) return;

            try
            {
                List<DatabasePlayer> selectedPlayers = playerDbDataGrid.SelectedItems.OfType<DatabasePlayer>().ToList();
                int removedCount = await Database.GetInstance().RemovePlayerListAsync(selectedPlayers);
                lock (DatabasePlayerDatagridLock)
                {
                    foreach (var player in selectedPlayers)
                    {
                        DbPlayerRepo.DatabasePlayersCollection.Remove(player);
                    }
                }

                ShowNoticeBar($"{removedCount}개의 플레이어 정보를 삭제했습니다.", Brushes.Green);
            }
            catch (Exception ex)
            {
                await this.ShowMessageAsync("ERROR", $"플레이어 삭제 중 오류가 발생했습니다.\n{ex.Message}", settings: GlobalDialogSettings);
            }
        }
    }
}