using System;
using System.ComponentModel;
using Microsoft.Graph.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using PiPic1.Helpers;
using PiPic1.Services;
using MSGraph.Response;
using MSGraph;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;

namespace PiPic1.Presentation;

public partial class SettingsViewModel : ObservableObject
{
    private readonly LoadImagesBackgroundworker _backgroundWorkerService;
    private INavigator _navigator;
    protected IMyService MyService { get; }
    
    public event EventHandler<string> TaskCompleted;
    public event EventHandler<int> ProgressUpdated;
    public ICommand LoadPicsCommand { get; }
    public ICommand NavigateToDesiredPage { get; }

    private string? _taskResult;
    public string? TaskResult
    {
        get => _taskResult;
        set
        {
            _taskResult = value;
        }
    }
    private string? _taskProgressString;
    public string? TaskProgressString
    {
        get => _taskProgressString;
        set
        {
            _taskProgressString = value;
        }
    }
    private bool _canExecute;
    public bool CanExecute
    {
        get => this._canExecute;
        set => this.SetProperty(ref this._canExecute, value);
    }
    private bool _isBusy;
    /// <summary>
    /// True, if ViewModel is busy, for Show Progress / Load Ring
    /// </summary>
    public bool IsBusy
    {
        get { return this._isBusy; }
        set { this.SetProperty(ref this._isBusy, value); }
    }
    private Setup setupSettings;
    public Setup SetupSettings
    {
        get { return this.setupSettings; }
        set { this.SetProperty(ref this.setupSettings, value); }
    }

    private ObservableCollection<TaskFolder> taskfolder = new ObservableCollection<TaskFolder>();
    public ObservableCollection<TaskFolder> MyOutlookTaskFolders
    {
        get { return this.taskfolder; }
        set { this.SetProperty(ref this.taskfolder, value); }
    }

    private TaskFolder selectedTaskFolder;
    public TaskFolder SelectedTaskFolder
    {

        get { return this.selectedTaskFolder; }
        set
        {
            this.SetProperty(ref this.selectedTaskFolder, value);
            System.Diagnostics.Debug.WriteLine("Selected Item " + selectedTaskFolder.Name + " id " + selectedTaskFolder.Id);
            this.SetupSettings.ToDoTaskListID = selectedTaskFolder.Id;
        }
    }
    public ICommand Submit { get; private set; }
    public ICommand GoToDashBoardPage{ get; }
    public ICommand LogInLogOut { get; private set; }

    private bool _isSignedIn;
    public bool IsSignedIn
    {
        get => _isSignedIn;
        set { 
            SetProperty(ref _isSignedIn, value);
            switch (_isSignedIn)
            {
                case true: _liginlogoutbtntxt = "Sign Out";break;
                case false: _liginlogoutbtntxt = "Sign In";break;
            }
        }
    }

    private string? _myUsername;
    public string? MyUsername
    {
        get => _myUsername;
        set => SetProperty(ref _myUsername, value);
    }

    private string _liginlogoutbtntxt;
    public string LogInLogOutBtnText
    {
        get => _liginlogoutbtntxt;

        set => SetProperty(ref _liginlogoutbtntxt, value);
    }
    private string _myToken;
    public string MyToken
    {
        get => _myToken;
        set => SetProperty(ref _myToken, value);
    }

    public SettingsViewModel(INavigator navigator, IMyService myservice, LoadImagesBackgroundworker backgroundWorkerService)
    {
        var dispatcherQueue = (App.Current as App).DispatcherQueue;
        if (dispatcherQueue == null)
        {
            throw new InvalidOperationException("DispatcherQueue is null. Ensure this method is called from the UI thread.");
        }
        dispatcherQueue.TryEnqueue(() =>
        {

            App.MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        });
        _navigator = navigator;
        MyService = myservice;
        LoadPicsCommand = new AsyncRelayCommand(OnLoadPics);
        Submit = new AsyncRelayCommand(OnSaveSettings);
        LogInLogOut = new AsyncRelayCommand(OnLoginLogout);
        GoToDashBoardPage = new AsyncRelayCommand(OnNavigateToDashBoardPage);
        //InitializeBackgroundWorker();
        _backgroundWorkerService = backgroundWorkerService;
        //_backgroundWorkerService.DispatcherQueue = _dispatcherQueue;
        _backgroundWorkerService.StatusUpdated += OnStatusUpdated;
        _backgroundWorkerService.TaskCompleted += OnLoadPicturesTaskCompleted;
        LoadData();
    }

    /// <summary>
    /// Load Initial Settings /Setup Data for the ViewModel
    /// </summary>
    public async Task LoadData()
    {
       
        AppConstants.LoadPictureListManually = false;
        _canExecute = false;
        IsBusy = true;
        try
        {
            System.Diagnostics.Debug.WriteLine("Get Settings From Sqlite " + AppConstants.DatabasePath);
            SetupSettings = await DAL.AppDataBase.GetSetup();
            IList<TaskFolder> myfolderlist = await MyService.GetTaskFolderFromGraph();
            taskfolder = myfolderlist.ToObservableCollection();
            selectedTaskFolder = myfolderlist.FirstOrDefault(t => t.Id == setupSettings.ToDoTaskListID);
            await LogIn();
            UpdateUI();

        }
        catch (Exception ex) { }
        finally
        {
            _canExecute = true;
        }
    }

    public async Task OnLoadPics()
    {
        System.Diagnostics.Debug.WriteLine("On Load Pictures");
        TaskResult = "Loading Pictures ... ";
        TaskProgressString = "";
        _backgroundWorkerService.StartImmediateTask();
    }

    /// <summary>
    /// Save Settings in Database if CanExecute = true
    /// </summary>
    private async Task OnSaveSettings()
    {
        //TODO: Check if can save 
        //e.g. cant save if background tasks interval are smaller than 15 minutes 

        if (SetupSettings.OneDrivePictureFolder=="")
            _canExecute = false;
        IsBusy = true;
        if (_canExecute == false)
        {
            System.Diagnostics.Debug.WriteLine("Can't save Settings");
            return;
        }
        else
        {

            System.Diagnostics.Debug.WriteLine("OnSaveSettings()");
            //IsBusy = true; //Causes : StackOverflowException 
            try
            {
                //IsBusy = true; // => StackOverflowException 
                await Task.Delay(2000);//TODO: Simulate Loading
                this.SetupSettings.ToDoTaskListName = SelectedTaskFolder.Name;
                await DAL.AppDataBase.UpdateSetup(this.SetupSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            finally
            {
                IsBusy = false;
                _canExecute = true;
            }
        }
    }

    #region Backgroundworker
    public async Task StartBackgroundTask()
    {
        _backgroundWorkerService.Start();
    }

    public async Task CancelBackgroundTask()
    {
        _backgroundWorkerService.Stop();
    }
    private async void OnStatusUpdated(object sender, string status)
    {
        TaskProgressString = status + " % geladen";
        if (status == "100")
        {
            _backgroundWorkerService.Stop();
            _taskResult = "Picture List Loaded";
        }
        UpdateUI();
        System.Diagnostics.Debug.WriteLine("Status : " + status + " % geladen");
    }

    private void OnLoadPicturesTaskCompleted(object sender, string taskStatus)
    {
        //TaskResult = taskStatus;
        System.Diagnostics.Debug.WriteLine("OnCompleted Picturesloaded , Result" + taskStatus);
        UpdateUI();
    }
    #endregion

    #region graph services

    /// <summary>
    /// Save Settings in Database if CanExecute = true
    /// </summary>
    private async Task OnLoginLogout()
    {
        switch (_isSignedIn)
        {
            case true: 
                await LogOut();

                break;
            case false: 
                await LogIn();
                break;
        }

        UpdateUI();
    }
   
    public async Task LogOut()
    {
        await GraphService.SignOut();
        _myUsername = "";
        _isSignedIn = false;
        _liginlogoutbtntxt = "Sign In";
        UpdateUI();
    }

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
            System.Diagnostics.Debug.WriteLine("Access Token " + myAuthResult.Account.Username);
            _myUsername = myAuthResult.Account.Username;
            _isSignedIn = true;
            _liginlogoutbtntxt = "Sign Out";
        }

        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
            return;
        }

    }
    #endregion

    private async Task OnNavigateToDashBoardPage()
    {
        //await _navigator.NavigateRouteForResultAsync(this,"Kiosk","");
        //_ = _navigator.NavigateViewModelAsync<KioskViewModel>(this);
        var dispatcherQueue = (App.Current as App).DispatcherQueue;
        if (dispatcherQueue == null)
        {
            throw new InvalidOperationException("DispatcherQueue is null. Ensure this method is called from the UI thread.");
        }
        dispatcherQueue.TryEnqueue(() =>
        {

            App.MainWindow.AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        });
        await _navigator.NavigateViewModelAsync<KioskViewModel>(this, Qualifiers.ClearBackStack);
    }

    private async void UpdateUI()
    {
        var dispatcherQueue = (App.Current as App).DispatcherQueue;
        if (dispatcherQueue == null)
        {
            throw new InvalidOperationException("DispatcherQueue is null. Ensure this method is called from the UI thread.");
        }
        dispatcherQueue.TryEnqueue(() => {
            OnPropertyChanged("TaskResult");
            OnPropertyChanged("TaskProgressString");
            OnPropertyChanged("MyOutlookTaskFolders");
            OnPropertyChanged("SelectedTaskFolder");
            OnPropertyChanged("IsSignedIn");
            OnPropertyChanged("LogInLogOutBtnText");
            OnPropertyChanged("MyUsername");
            OnPropertyChanged("CanExecute");
        });
    }
}
