using Nokk.Controls;
using Nokk.Icons;
using Mages.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace Nokk
{
    public partial class MainWindow : Window
    {
        #region VARS
        private readonly string COMMANDS_PATH = $"{Environment.CurrentDirectory}\\Commands.txt";
        private readonly string APPS_FOLDERS_PATH = $"{Environment.CurrentDirectory}\\AppsFolders.txt";

        private string[] commands;
        private static string[] AppsFoldersPaths;

        public int ListBoxItemHeight { get; set; } = 50;
        private ObservableCollection<SearchResultItem> SearchResultItems = new ObservableCollection<SearchResultItem>();
        private ObservableCollection<object> VolumeMixerItems = new ObservableCollection<object>();

        private Tabs CurrentTab;

        string lastChangedVolume;
        Process[] GroupOfProcesses = new Process[] { };
        private ProcessWindow[] runningApps;
        private string[] AppPaths;
        #endregion
        #region Окна
        private enum Tabs
        {
            Main,
            Apps,
            Games
        }
        #endregion
        #region Настройки перевода
        private enum TranslateOption
        {
            enru,
            ruen
        }
        #endregion
        #region
        public MainWindow()
        {
            InitializeComponent();
            lstBox.DataContext = this;
        }
        #endregion

        ////// Обработчики событий //////

        private void MyNotifyIcon_TrayMouseUp(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Hidden)
            {
                Show();
                txb.Text = "";
                SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
            }
            else Hide();
        }

        #region При нажатии на окно
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        #endregion

        #region Выделение элементов списка при наведении на них
        private void lb_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (null == listBox)
            {
                return;
            }

            var point = e.GetPosition((UIElement)sender);

            VisualTreeHelper.HitTest(listBox, null, (hitTestResult) =>
            {
                var uiElement = hitTestResult.VisualHit as UIElement;

                while (null != uiElement)
                {
                    ListBoxItem listBoxItem = uiElement as ListBoxItem;
                    if (null != listBoxItem)
                    {
                        listBoxItem.IsSelected = true;
                        return HitTestResultBehavior.Stop;
                    }

                    uiElement = VisualTreeHelper.GetParent(uiElement) as UIElement;
                }

                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(point));
        }
        #endregion

        #region При загрузке окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentTab = Tabs.Main;

            commands = File.ReadAllLines(COMMANDS_PATH);

            Init();
        }
        #endregion

        #region При изменении текста поля поиска
        private void Txb_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateResult();
        }
        #endregion

        #region Команды
        private void lstBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstBox.SelectedIndex > -1)
            {
                if (!txb.Text.Contains("!"))
                {
                    switch (SearchResultItems[lstBox.SelectedIndex].Header.Text)
                    {
                        case "Перевести текст":
                            {
                                ViewTranslator();
                                break;
                            }
                        case "Приложения":
                            {
                                CurrentTab = Tabs.Apps;
                                ViewApps();
                                break;
                            }
                        case "Калькулятор":
                            {
                                ViewCalc();
                                break;
                            }
                        case "Микшер громкости":
                            {
                                ViewVolumeMixer();
                                break;
                            }
                        case "Выход":
                            {
                                Window.Close();
                                break;
                            }
                    }
                    if (CurrentTab == Tabs.Apps && lstBox.SelectedIndex > -1)      // Запуск приложений
                    {
                        Process.Start(AppPaths[lstBox.SelectedIndex]);
                    }

                }
                else if (lstBox.Items.Count > 0)
                {
                    if (txb.Text.ToLower().StartsWith("c!"))
                    {
                        Clipboard.SetText(Calculate().ToString());
                    }
                    else if (txb.Text.ToLower().StartsWith("t!"))
                    {
                        Clipboard.SetText(SearchResultItems[0].Header.Text.ToString());
                    }
                }

            }

        }
        #endregion
        private void txb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                #region Open URL or files
                if (Regex.IsMatch(txb.Text, @"^(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$") || Regex.IsMatch(txb.Text, @"(\\\\?([^\\/]*[\\/])*)([^\\/]+)$"))
                {
                    try
                    {
                        Process.Start(txb.Text);
                    }
                    catch
                    {
                        try
                        {
                            Process.Start($"www.{txb.Text} ");
                        }
                        catch
                        {
                            lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(ItemImage: IconManager.error, ItemHeader: "ERROR", ItemDescription: "Упс, что - то пошло не так:(") };
                        }
                    }
                }
                #endregion 
                #region CMD
                else if (txb.Text.Contains(">"))
                {
                    try
                    {
                        ProcessStartInfo psi;
                        psi = new ProcessStartInfo()
                        {
                            UseShellExecute = true,
                            WorkingDirectory = @"C:\Windows\System32",
                            FileName = @"C:\Windows\System32\cmd.exe",
                            Arguments = "/k " + txb.Text.Replace(">", "")
                        };
                        Process.Start(psi);
                    }
                    catch
                    {
                        lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(ItemImage: IconManager.error, ItemHeader: "ERROR", ItemDescription: "Упс, что - то пошло не так:(") };
                    }
                }
                #endregion
            }
        }

        ////// Методы //////
        #region Пути до приложений
        private string[] GetAppPaths()
        {
            SearchResultItems.Clear();
            List<string> result = new List<string>();
            for (int i = 0; i < AppsFoldersPaths.Length; i++)
            {
                result.AddRange(SearchApps("exe", AppsFoldersPaths[i]));
                result.AddRange(SearchApps("lnk", AppsFoldersPaths[i]));
                result.AddRange(SearchApps("url", AppsFoldersPaths[i]));
            }
            return result.ToArray();
        }

        private List<String> SearchApps(string extension, string path)
        {
            List<string> result = new List<string>();
            foreach (string file in Directory.EnumerateFiles(path + "\\", $"*.{extension}"))
            {
                result.Add($"{path}\\{Path.GetFileName(file)}");
            }
            return result;
        }
        #endregion

        #region Получить изображение по путю
        public static BitmapImage GetImage(string path)
        {
            return new BitmapImage(new Uri($"{Environment.CurrentDirectory}\\{path}"));
        }
        #endregion

        #region Обновить результат поиска
        private void UpdateResult()
        {
            SearchResultItems.Clear();
            if (!string.IsNullOrEmpty(txb.Text))
            {
                if (!txb.Text.Contains("!") && !txb.Text.StartsWith(">") && !Regex.IsMatch(txb.Text.Replace(" ", ""), @"^\d+\D{3}"))
                {
                    if (Regex.IsMatch(txb.Text, @"^(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$") || Regex.IsMatch(txb.Text, @"(\\\\?([^\\/]*[\\/])*)([^\\/]+)$"))
                    {
                        lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(ItemImage: IconManager.www, ItemHeader: "Открыть сайт или файл", ItemDescription: txb.Text) };
                    }
                    else
                    {
                        for (int i = 0; i < commands.Length; i++) // Поиск и вывод команд
                        {
                            Match match = Regex.Match(commands[i], $@"\w*{txb.Text}\w*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.execute, ItemHeader: commands[i], ItemDescription: "Нажмите, чтобы выполнить"));
                            }
                        }
                        lstBox.ItemsSource = SearchResultItems;
                    }
                }
                else if (txb.Text.ToLower().StartsWith("c!")) // Калькулятор
                {
                    lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(IconManager.calculator, Calculate().ToString(), "Нажмите, чтобы скопировать") };
                }
                else if (txb.Text.StartsWith(">")) // cmd
                {
                    lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(ItemImage: IconManager.cmd, ItemHeader: txb.Text, ItemDescription: $"Выполнить {txb.Text.Replace(">", "")}") };
                }
                else if (InternetConnectionAvailable() && (Regex.IsMatch(txb.Text.Replace(" ", ""), @"^[+-]?([0-9]*[.])?[0-9]+\D{3}") || txb.Text.ToLower().StartsWith("t!")))
                {
                    if (Regex.IsMatch(txb.Text.Replace(" ", ""), @"^[+-]?([0-9]*[.])?[0-9]+\D{3}"))
                    {
                        ViewCurrencyConverter();
                    }
                    else if (txb.Text.ToLower().StartsWith("t!"))
                    {
                        SearchResultItems.Clear();
                        try
                        {
                            SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.translator,
                                                                                              ItemHeader: Translator.Translate(txb.Text.Remove(0, 2)),
                                                                                              ItemDescription: "Нажмите, чтобы скопировать"));
                        }
                        catch
                        {
                            SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.error,
                                                                                          ItemHeader: "Возможно ваш API key некорректен",
                                                                                          ItemDescription: "Чтобы новый ключ вошел в силу - перезагрузите приложение"));
                        }
                        lstBox.ItemsSource = SearchResultItems;
                    }
                }
                else if (!InternetConnectionAvailable())
                {
                    lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(ItemImage: IconManager.error,
                                                                                          ItemHeader: "Проверьте подключение к интернету",
                                                                                          ItemDescription: "Ошибка") };
                }
            }
            else if (string.IsNullOrEmpty(txb.Text))
            {
                ViewAllCommands();
            }
        }
        #endregion

        #region Отобразить все команды
        private void ViewAllCommands()
        {
            SearchResultItems.Clear();
            for (int i = 0; i < commands.Length; i++)
            {
                SearchResultItems.Add(GetSearchResultItem(IconManager.execute, commands[i], "Нажмите, чтобы выполнить"));
            }
            lstBox.ItemsSource = SearchResultItems;
        }
        #endregion

        #region Установить положение окна
        private void SetWindowLocation()
        {
            // Центр по горизонтали
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            //Немного отступить сверху
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 8;
        }
        #endregion

        #region Пути до папок приложений
        private void CreateAppFoldersPaths()
        {
            string[] lines = File.ReadAllLines(APPS_FOLDERS_PATH);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("%user%"))
                {
                    lines[i] = lines[i].Replace("%user%", Environment.UserName);
                }
            }
            File.WriteAllLines(APPS_FOLDERS_PATH, lines);
        }
        #endregion

        #region Инициализация
        private void Init()
        {
            CreateAppFoldersPaths();
            AppsFoldersPaths = File.ReadAllLines(APPS_FOLDERS_PATH);
            AppPaths = GetAppPaths();
            // Фокус на поле поиска при входе в программу
            txb.Focus();
            // Отображение всех команд при входе в программу
            ViewAllCommands();
            // Позиционирование
            SetWindowLocation();
        }

        #endregion
        public void ViewCurrencyConverter()
        {
            SearchResultItems.Clear();
            string[] symbols = new string[2];
            double value = Convert.ToDouble(Regex.Match(txb.Text.Replace(",", "."), @"^[+-]?([0-9]*[.])?[0-9]+").Value);
            switch (Regex.Match(txb.Text.Replace(" ", ""), @"\D{3}").Value.ToUpper())
            {
                case "USD":
                    symbols[0] = "RUB";
                    symbols[1] = "EUR";
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("USD", value, symbols).rates.RUB * value}₽)",
                                                             ItemDescription: "Нажмите, чтобы скопировать"));
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("USD", value, symbols).rates.EUR * value}€",
                                                             ItemDescription: "Нажмите, чтобы скопировать"));
                    break;
                case "EUR":
                    symbols[0] = "RUB";
                    symbols[1] = "USD";
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("EUR", value, symbols).rates.RUB * value}₽",
                                                              ItemDescription: "Нажмите, чтобы скопировать"));
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("EUR", value, symbols).rates.USD * value}$",
                                                             ItemDescription: "Нажмите, чтобы скопировать"));
                    break;
                case "RUB":
                    symbols[0] = "USD";
                    symbols[1] = "EUR";
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("RUB", value, symbols).rates.EUR * value}€",
                                                             ItemDescription: "Нажмите, чтобы скопировать"));
                    SearchResultItems.Add(GetSearchResultItem(ItemImage: IconManager.currencyConverter, ItemHeader: $"= {CurrencyConverter.GetRates("RUB", value, symbols).rates.USD * value}$",
                                                             ItemDescription: "Нажмите, чтобы скопировать"));
                    break;
            }
            lstBox.ItemsSource = SearchResultItems;
        }
        #region Проверить интернет соединение
        public static bool InternetConnectionAvailable()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }
        #endregion

        #region Получить предмет результата поиска
        private SearchResultItem GetSearchResultItem(string ItemImage, string ItemHeader, string ItemDescription) => new SearchResultItem() { image = GetImage(ItemImage), header = ItemHeader, description = ItemDescription };
        #endregion

        ////// Команды //////


        #region Отобразить приложения
        private void ViewApps()
        {
            SearchResultItems.Clear();
            for (int i = 0; i < AppPaths.Length; i++)
            {
                SearchResultItems.Add(GetSearchResultItem(IconManager.defaultIcon, Path.GetFileName(AppPaths[i]), AppPaths[i]));
            }
            lstBox.ItemsSource = SearchResultItems;
        }
        #endregion

        #region Переводчик
        private void ViewTranslator()
        {
            txb.Text = "t!";
            lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(IconManager.translator, "Начните вводить текст", "и отобразится перевод") };
            txb.Focus();
            txb.SelectionStart = txb.Text.Length;
        }
        #endregion

        #region  Калькулятор
        private void ViewCalc()
        {
            txb.Text = "c!";
            lstBox.ItemsSource = new List<SearchResultItem> { GetSearchResultItem(IconManager.calculator, $"= {Calculate().ToString()}", "Нажмите, чтобы скопировать") };
            txb.Focus();
        }


        #region Вычислить
        private object Calculate()
        {
            double result;
            if (txb.Text.Length > 2)
            {
                try
                {
                    Engine engine = new Engine();
                    string equation = txb.Text.Remove(0, 2);
                    result = (double)engine.Interpret(equation);
                    return result;
                }
                catch
                {
                    return "Ошибка";
                }

            }
            else
            {
                return 0;
            }
        }
        #endregion
        #endregion

        #region Микшер громкости
        private void ViewVolumeMixer()
        {
            runningApps = ProcessHelper.GetRunningApplications();

            VolumeMixerItems.Clear();

            uint counter = 0;
            foreach (ProcessWindow pw in runningApps)
            {
                if (pw.WindowTitle != "Microsoft Text Input Application")
                {
                    counter++;
                    VolumeMixerItems.Add(GetSearchResultItem(ItemImage: IconManager.defaultIcon, ItemHeader: pw.WindowTitle, ItemDescription: $"{pw.Process.ProcessName} ► {pw.Process.MainModule.FileName}"));
                    var slider = new Slider
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Width = lstBox.ActualWidth,
                        IsMoveToPointEnabled = true,
                        Name = $"slider{counter}",
                        Value = 50
                    };
                    slider.ValueChanged += VolumeSlider_ValueChanged;
                    VolumeMixerItems.Add(slider);
                }
                else continue;
            }
            lstBox.ItemsSource = VolumeMixerItems;
        }
        private void SearchDublicateProcesses(string name)
        {
            GroupOfProcesses = Process.GetProcessesByName(name);
        }

        private void SetGroupOfAppsVolume(int value)
        {
            foreach (Process p in GroupOfProcesses)
            {
                AudioManager.SetApplicationVolume(p.Id, value);
            }
        }
        public void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            int sliderPosition = Convert.ToInt32(slider.Name.Remove(0, 6)) - 1;
            if (lastChangedVolume == Process.GetProcessById(GetPIDBySliderPosition(sliderPosition)).ProcessName)
            {
                SetGroupOfAppsVolume((int)slider.Value);
            }
            else
            {
                SearchDublicateProcesses(Process.GetProcessById(GetPIDBySliderPosition(sliderPosition)).ProcessName);
                SetGroupOfAppsVolume((int)slider.Value);
            }
            lastChangedVolume = GroupOfProcesses[0].ProcessName;
        }

        #region Получить PID по номерy слайдерa
        private int GetPIDBySliderPosition(int SliderPosition)
        {
            int pid = runningApps[SliderPosition].Process.Id;
            return pid;
        }
        #endregion
        #endregion

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
    }
}