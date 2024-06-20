using Microsoft.UI.Xaml.Media.Imaging;
using PiPic1.Helpers;
using PiPic1.Services;
using Windows.UI.Core;

namespace PiPic1.Presentation;

public partial class SecondViewModel : ObservableObject
{
    private INavigator _navigator;
    protected IMyService MessageService { get; }
    private BitmapImage? _dashImage;
    private string? _pictureName;
    public ICommand ClickButton { get; }
    public BitmapImage? DashImage
    {
        get => _dashImage;
        set
        {
            _dashImage = value;
            OnPropertyChanged(nameof(DashImage));
        }
    }

    public string? DashImageDescription
    {
        get => _pictureName;
        set
        {
            _pictureName = value;
            OnPropertyChanged(nameof(DashImageDescription));
        }
    }

    public SecondViewModel(INavigator navigator, IMyService myservice)
    {
        _navigator = navigator;
        MessageService = myservice;
        ClickButton = new AsyncRelayCommand(LoadData);
    }

        /// <summary>
        /// Load Initial Settings /Setup Data for the ViewModel
        /// </summary>
        public async Task LoadData()
    {
        try
        {
            await this.UpdateDashBoardImageAsync();
            Timing.StartTimer(0, 60, async () => await this.UpdateDashBoardImageAsync());
        }
        catch (Exception ex) { }
    }

    /// <summary>
    /// Updates the Dashboard Image 
    /// </summary>
    private async Task UpdateDashBoardImageAsync()
    {
        try
        {
            var getimage = await MessageService.StreamImageFromOneDrive();
            if (getimage != null)
            {
                DashImage = getimage.Photo;
                DashImageDescription = getimage.Description;
            }
        }
        catch (Exception ex) { }
        finally
        {
            //UpdateUI();
        }

        try
        {
            //await LoadDatabaseInfos();
        }
        catch (Exception ex) { }
        finally
        {
            //UpdateUI();
        }
    }
}
