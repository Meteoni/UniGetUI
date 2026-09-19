using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UniGetUI.Avalonia.Infrastructure;
using UniGetUI.Core.Tools;

namespace UniGetUI.Avalonia.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        bool isDark = ThemeHelper.IsDark;
        string uri = isDark
            ? "avares://UniGetUI/Assets/SplashScreen.theme-dark.png"
            : "avares://UniGetUI/Assets/SplashScreen.png";
        SplashImage.Source = new Bitmap(AssetLoader.Open(new Uri(uri)));

        TaglineText.Text = CoreTools.Translate("Package management made easy");
        TaglineText.Foreground =
            Application.Current?.TryGetResource(
                "TextFillColorPrimaryBrush",
                ThemeHelper.Variant,
                out var foreground) == true && foreground is IBrush brush
                ? brush
                : isDark ? Brushes.White : Brushes.Black;
    }
}
