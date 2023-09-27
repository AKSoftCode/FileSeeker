using MudBlazor;

namespace FileSeeker.Shared
{
    public partial class MainLayout
    {
        bool _drawerOpen = false;
        private bool _isDarkMode;
        private MudThemeProvider? _mudThemeProvider;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isDarkMode = await _mudThemeProvider!.GetSystemPreference();
                _isDarkMode = true;
                StateHasChanged();
            }
        }

        void DrawerToggle()
        {
            _drawerOpen = !_drawerOpen;
        }
    }
}