namespace PiPic1.Presentation;

public class ShellViewModel
{
    private readonly INavigator _navigator;

    public ShellViewModel(
        INavigator navigator)
    {
        _navigator = navigator;
        _ = Start();
    }

    public async Task Start()
    {
        await _navigator.NavigateViewModelAsync<KioskViewModel>(this, Qualifiers.ClearBackStack);
        //await _navigator.NavigateViewModelAsync<SettingsViewModel>(this, Qualifiers.ClearBackStack);
    }
}
