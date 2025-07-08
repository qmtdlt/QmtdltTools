namespace QmtdltTools.MAUI.Blazor
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "QmtdltTools.MAUI.Blazor" };
        }
    }
}
