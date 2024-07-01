using System.ComponentModel;
using Microsoft.Graph.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using MSGraph;
using PiPic1.Services;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PiPic1.Presentation;

public partial class MainViewModel : ObservableObject
{
    private INavigator _navigator;
    protected IMyService MessageService { get; }
    private BitmapImage _dashImage;
    private BackgroundWorker _worker;
    public event EventHandler<string> TaskCompleted;
    public event EventHandler<int> ProgressUpdated;

    [ObservableProperty]
    private string? name;

    [ObservableProperty]
    private string? imglist;
    public string ImgList
    {
        get => imglist;
        set
        {
            imglist = value;
            OnPropertyChanged(nameof(ImgList));
        }
    }

    public string Message { get => MessageService.GetMessage(); }
    public BitmapImage DashImage
    {
        get => _dashImage;
        set
        {
            _dashImage = value;
            OnPropertyChanged(nameof(DashImage));
        }
    }

    public MainViewModel(
        INavigator navigator,IMyService myservice)
    {
        _navigator = navigator;
        MessageService = myservice;
        Title = "Main";
        GoToSecond = new AsyncRelayCommand(GoToSecondView);
        GoToSettings = new AsyncRelayCommand(GoToSettingsView);
        StartMyTask = new AsyncRelayCommand(StartBackgroundTask);
        StopMyTask = new AsyncRelayCommand(CancelBackgroundTask);
        SetupDatabase();
        InitializeBackgroundWorker();
        LogIn();
    }
    public string? Title { get; }
    public string? MyToken { get; private set; }

    public ICommand GoToSecond { get; }
    public ICommand GoToSettings { get; }
    public ICommand StartMyTask { get; }
    public ICommand StopMyTask { get; }

    public async Task LogIn()
    {
        try
        {
            var mytoken = await GraphService.GetTokenForUserAsync();
            System.Diagnostics.Debug.WriteLine("Access Token " + mytoken);
#if __ANDROID__
            Android.Util.Log.Debug("YourAppName", "Access Token " + mytoken);
#else
                System.Diagnostics.Debug.WriteLine("Access Token " + mytoken);
#endif

            var myAuthResult = await GraphService.GetAuthResult();
            System.Diagnostics.Debug.WriteLine("Account.Username " + myAuthResult.Account.Username);
            //_myUsername = myAuthResult.Account.Username;
            MyToken = mytoken;
            if (String.IsNullOrEmpty(MyToken)) { 
                StartBackgroundTask(); 
            }
           
        }

        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
            return;
        }

    }

    private async Task GoToSecondView()
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: new PiPic1.Models.Entity(Name!));
    }

    private async Task GoToSettingsView()
    {
        _ = _navigator.NavigateViewModelAsync<SettingsViewModel>(this);
    }

    private void SetupDatabase()
    {
        DAL.AppDataBase.InitializeAsync();
    }

    private void InitializeBackgroundWorker()
    {
        // Initialize BackgroundWorker
        _worker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        _worker.DoWork += BackgroundWorker_DoWork;
        _worker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        _worker.ProgressChanged += BackgroundWorker_ProgressChanged;
    }

        public async Task StartBackgroundTask()
    {
        if (!_worker.IsBusy)
        {
            _worker.RunWorkerAsync();
        }
    }

    public async Task CancelBackgroundTask()
    {
        if (_worker.IsBusy)
        {
            _worker.CancelAsync();
        }
    }

    private async void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        //await LoadImageListFromOneDrive();

        // Perform your background work here
        var result = await MessageService.LoadImageListFromOneDrive();
        e.Result = result;
    }

    private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        // Handle the completion of the background work
        var result = "Task completed";

        if (e.Cancelled)
        {
            result = "Task cancelled";
        }
        else if (e.Error != null)
        {
            result = $"Task failed: {e.Error.Message}";
        }

            TaskCompleted?.Invoke(this, result);
    }

    private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
            ProgressUpdated?.Invoke(this, e.ProgressPercentage);
    }

}
