namespace FileSeeker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            if(window is not null)
            {
                window.Title = "File Seeker";
            }

            return window!;
        }
    }
}