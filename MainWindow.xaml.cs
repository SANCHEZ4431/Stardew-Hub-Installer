using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Interop;

namespace StardewHubInstaller
{
    public partial class MainWindow : Window
    {
        int currentPage = 1;

        // WinAPI для эффекта Glass/Aero
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        public MainWindow()
        {
            InitializeComponent();
            PathBox.Text = @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley";
            ShowPage(1);
            this.MouseDown += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
            LangBox.SelectedIndex = 0;
            ApplyLocalization(LangBox.SelectedIndex == 0);
        }

        private void ShowPage(int p, bool isForward = true)
        {
            Grid[] pages = { LanguagePage, Page1, Page2, Page3, Page4, Page5, Page6 };
            TranslateTransform[] transforms = { LangTransform, Page1Transform, Page2Transform, Page3Transform, Page4Transform, Page5Transform, Page6Transform };

            for (int i = 0; i < pages.Length; i++)
            {
                bool isCurrent = (i == p - 1);
                pages[i].Visibility = isCurrent ? Visibility.Visible : Visibility.Collapsed;

                if (isCurrent)
                    AnimatePage(pages[i], transforms[i], isForward);
            }

            // Навигация
            BackBtn.Visibility = (p == 1 || p == 6) ? Visibility.Collapsed : Visibility.Visible;

            if (p == 5) UpdateSummary();
            if (p == 6) StartInstallation();

            UpdateButtonTexts();
        }

        private void UpdateButtonTexts()
        {
            if (NextBtn == null || BackBtn == null) return;

            bool isEng = LangBox.SelectedIndex == 0;

            if (currentPage == 7)
                NextBtn.Content = isEng ? "Finish" : "Завершить";
            else if (currentPage == 6)
                NextBtn.Content = isEng ? "Installing..." : "Установка...";
            else if (currentPage == 5) // Страница перед прогресс-баром
                NextBtn.Content = isEng ? "Install" : "Установить";
            else
                NextBtn.Content = isEng ? "Next" : "Далее";

            BackBtn.Content = isEng ? "Back" : "Назад";
        }

        private void ApplyLocalization(bool isEng)
        {
            // Проверка любого элемента с первой страницы (например, TxtSelectLang)
            if (TxtSelectLang == null) return;

            // Page 1 - Select Language
            TxtSelectLang.Text = isEng ? "Select Language" : "Выберите язык";
            TxtSelectLangSub.Text = isEng ? "Select the installation language" : "Выберите язык для установки";

            // Page 2 - Welcome
            if (TxtWelcome != null)
            {
                TxtWelcome.Text = isEng ? "Welcome" : "Добро пожаловать";
                TxtWelcomeSub.Text = isEng ? "This wizard will help you install the application." : "Этот мастер поможет вам установить приложение.";
            }

            // Page 3 - Install Folder
            if (TxtPathTitle != null)
            {
                TxtPathTitle.Text = isEng ? "Choose Install Folder" : "Выберите папку установки";
                PathBoxSelect.Content = isEng ? "Browse..." : "Обзор...";
            }

            // Page 4 - Additional Options
            if (TxtOptionsTitle != null)
            {
                TxtOptionsTitle.Text = isEng ? "Additional Options" : "Дополнительные параметры";
                DotNetCheck.Content = isEng ? "Install .NET Runtime 8.0 (recommended)" : "Установить .NET Runtime 8.0 (рекомендуется)";
            }

            // Page 5 - Summary
            if (TxtSummaryTitle != null)
            {
                TxtSummaryTitle.Text = isEng ? "Ready to Install" : "Все готово к установке";
                // Здесь можно добавить сводку:
                SummaryText.Text = isEng ? "Click 'Install' to begin." : "Нажмите 'Установить', чтобы начать.";
            }

            // Page 6 - Installing
            if (StatusLabel != null)
            {
                StatusLabel.Text = isEng ? "Starting installation..." : "Запуск установки...";
            }

            // Page 7 - Finished
            if (TxtFinalTitle != null)
            {
                TxtFinalTitle.Text = isEng ? "Installation Complete!" : "Установка завершена!";
                ShortcutCheck.Content = isEng ? "Create Desktop Shortcut" : "Создать ярлык на рабочем столе";
                RunCheck.Content = isEng ? "Run Stardew Hub now" : "Запустить Stardew Hub сейчас";
                if (ReadmeCheck != null) ReadmeCheck.Content = isEng ? "Open Readme.txt" : "Открыть Readme.txt";
            }

            UpdateButtonTexts();
        }

        private void LangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || TxtSelectLang == null) return;

            ApplyLocalization(LangBox.SelectedIndex == 0);
        }

        private void LangBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!LangBox.IsDropDownOpen)
            {
                LangBox.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private void AnimatePage(Grid page, TranslateTransform transform, bool isForward)
        {
            double startPos = isForward ? 100 : -100;
            transform.X = startPos;
            page.Opacity = 0;

            DoubleAnimation slide = new DoubleAnimation
            {
                From = startPos,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(450),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(450)
            };

            transform.BeginAnimation(TranslateTransform.XProperty, slide);
            page.BeginAnimation(Grid.OpacityProperty, fade);
        }

        private void UpdateSummary()
        {
            bool isEng = LangBox.SelectedIndex == 0;
            SummaryText.Text = (isEng ? "Destination folder:\n" : "Папка назначения:\n") + PathBox.Text + "\n\n" +
                               (isEng ? "Options:\n" : "Дополнительно:\n") +
                               $"• {(DotNetCheck.IsChecked == true ? (isEng ? "Install .NET Runtime" : "Установка .NET Runtime") : (isEng ? "Skip .NET" : "Пропустить .NET"))}";
        }

        private async void StartInstallation()
        {
            NextBtn.IsEnabled = false;
            string installPath = PathBox.Text;
            string tempNetPath = Path.Combine(Path.GetTempPath(), "windowsdesktop-runtime.exe");
            bool isEng = LangBox.SelectedIndex == 0;

            try
            {
                if (!Directory.Exists(installPath)) Directory.CreateDirectory(installPath);

                var assembly = Assembly.GetExecutingAssembly();
                string[] resourceNames = assembly.GetManifestResourceNames();

                // ШАГ 1: .NET
                if (DotNetCheck.IsChecked == true)
                {
                    StatusLabel.Text = isEng ? "Extracting .NET..." : "Извлечение .NET Runtime...";
                    string? netRes = Array.Find(resourceNames, r => r.Contains("windowsdesktop-runtime.exe", StringComparison.OrdinalIgnoreCase));
                    if (netRes != null)
                    {
                        await ExtractResource(assembly, netRes, tempNetPath);
                        StatusLabel.Text = isEng ? "Installing .NET (please wait)..." : "Установка .NET (ожидание)...";
                        await Task.Run(() => {
                            ProcessStartInfo psi = new ProcessStartInfo(tempNetPath, "/passive /norestart") { UseShellExecute = true, Verb = "runas" };
                            Process.Start(psi)?.WaitForExit();
                        });
                        if (File.Exists(tempNetPath)) File.Delete(tempNetPath);
                    }
                }

                // ШАГ 2: Файлы
                string[] appFiles = { "StardewHub.exe", "StardewHub.dll", "StardewHub.runtimeconfig.json", "StardewHub.deps.json", "Readme.txt" };
                for (int i = 0; i < appFiles.Length; i++)
                {
                    StatusLabel.Text = (isEng ? "Copying " : "Копирование ") + appFiles[i] + "...";
                    string? resName = Array.Find(resourceNames, r => r.EndsWith(appFiles[i], StringComparison.OrdinalIgnoreCase));
                    if (resName != null)
                    {
                        await ExtractResource(assembly, resName, Path.Combine(installPath, appFiles[i]));
                    }

                    double progress = ((double)(i + 1) / appFiles.Length) * 100;
                    InstallBar.BeginAnimation(ProgressBar.ValueProperty, new DoubleAnimation(progress, TimeSpan.FromMilliseconds(200)));
                    await Task.Delay(100);
                }

                currentPage = 7;
                ShowPage(7);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { NextBtn.IsEnabled = true; }
        }

        private async Task ExtractResource(Assembly assembly, string resName, string destPath)
        {
            using (Stream? s = assembly.GetManifestResourceStream(resName))
            {
                if (s == null)
                {
                    throw new FileNotFoundException($"Resource '{resName}' not found in assembly.");
                }

                using (FileStream fs = new FileStream(destPath, FileMode.Create))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage == 6) return;
            if (currentPage == 7)
            {
                string exe = Path.Combine(PathBox.Text, "StardewHub.exe");
                string readmePath = Path.Combine(PathBox.Text, "Readme.txt");
                if (ShortcutCheck.IsChecked == true) CreateDesktopShortcut(exe);
                if (ReadmeCheck.IsChecked == true && File.Exists(readmePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(readmePath) { UseShellExecute = true });
                    }
                    catch { }
                }
                if (RunCheck.IsChecked == true && File.Exists(exe)) Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                this.Close();
                return;
            }
            currentPage++;
            ShowPage(currentPage, true);
        }

        private void Back_Click(object sender, RoutedEventArgs e) { currentPage--; ShowPage(currentPage, false); }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { ValidateNames = false, CheckFileExists = false, CheckPathExists = true, FileName = "Folder Selection" };
            if (dialog.ShowDialog() == true) PathBox.Text = Path.GetDirectoryName(dialog.FileName);
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); DragMove(); }

        private void CreateDesktopShortcut(string targetExePath)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "Stardew Hub.lnk");
                IShellLinkW shortcut = (IShellLinkW)new ShellLink();
                shortcut.SetPath(targetExePath);
                string? workingDir = Path.GetDirectoryName(targetExePath);
                shortcut.SetWorkingDirectory(workingDir ?? string.Empty);

                ((IPersistFile)shortcut).Save(shortcutPath, false);
            }
            catch { }
        }

        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = 0x01000000;

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void AnimatePage(Grid page, TranslateTransform transform)
        {
            page.Opacity = 0;
            transform.X = 20;

            DoubleAnimation fade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(400));
            DoubleAnimation slide = new DoubleAnimation(0, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            page.BeginAnimation(Grid.OpacityProperty, fade);
            transform.BeginAnimation(TranslateTransform.XProperty, slide);
        }
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

}
