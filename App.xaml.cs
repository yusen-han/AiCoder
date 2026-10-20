using AiCoder.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiCoder
{
    public partial class App : Application
    {
        public App()
        {
            LocalizationService.Initialize();
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Destroying += async (_, _) =>
            {
                if (window.Page is AppShell shell && shell.CurrentPage is MainPage mainPage)
                    await mainPage.SaveSessionsAsync();
            };
            return window;
        }
    }
}
