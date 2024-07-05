
using System.ComponentModel;

namespace PiPic1.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class KioskPage : Page
{
    public KioskViewModel ViewModel { get; set; }
    public KioskPage()
    {
        this.InitializeComponent();
        //this.ViewModel = (App.Current as App).Host.Services.GetDefaultInstance<KioskViewModel>();
        //ViewModel.LoadData();
    }

    /// <summary>
    /// Loads the saved Dashboard data on first navigation
    /// </summary>
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //await ViewModel.LoadData();
    }
}
