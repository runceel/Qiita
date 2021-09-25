using boilersGraphics.Views;
using Prism.Ioc;
using Prism.Unity;
using System.Reflection;
using System.Windows;

namespace boilersGraphics
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : PrismApplication
    {
        public static bool IsTest { get; set; }

        protected override Window CreateShell()
        {
            var w = Container.Resolve<MainWindow>();
            return w;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        public static string GetAppNameAndVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var majorMinorBuild = $"{version.Major}.{version.Minor}.{version.Build}";
            var appnameAndVersion = $"MINIMALIZED boiler's Graphics {majorMinorBuild}";
            return appnameAndVersion;
        }
    }
}