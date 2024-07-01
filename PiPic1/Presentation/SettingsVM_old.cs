using MSGraph;
using MSGraph.Response;
using System.Collections.ObjectModel;
using PiPic1.Helpers;
using PiPic1.Services;
using System.ComponentModel;
using Windows.UI.Core;
using static SQLite.SQLite3;
using Windows.ApplicationModel.Core;
using Microsoft.Graph.Models;
using Microsoft.UI.Dispatching;

namespace PiPic1.Presentation;

public partial class SettingsVM_old : ObservableObject
{
    #region Fields
    private ObservableCollection<TaskFolder> taskfolder = new ObservableCollection<TaskFolder>();
    private BackgroundWorker _worker;
    public ObservableCollection<TaskFolder> TaskFolders { get; } = new ObservableCollection<TaskFolder>();
    private TaskFolder selectedTaskFolder;
    private ObservableCollection<TaskResponse> taskList = new ObservableCollection<TaskResponse>();
    private TaskResponse selectedPurchaseTask;
    private readonly LoadImagesBackgroundworker _backgroundWorkerService;
    private bool canExecute;
    private Setup setupSettings;
    private bool _isBusy;
    private string _taskResult;
    private string _taskStatus;
    private int _taskProgress;
    //private string _taskProgressString;
    private string? _myUsername;
    //private readonly Services.GetImageListFromOneDriveBGService _backgroundWorkerService;
    private string _status;
    private INavigator _navigator;
    protected IMyService MessageService { get; }
    
    public ICommand NavigateToDesiredPage { get; }
    public ICommand SwitchToogled { get; }

    private DispatcherQueue dispatcherQueue => DispatcherQueue.GetForCurrentThread();
    #endregion

    #region Properties
   
    public string? MyUsername
    {
        get => _myUsername;
        set
        {
            _myUsername = value;
            OnPropertyChanged(nameof(MyUsername));
        }
    }
    public int TaskProgress
    {
        get { return this._taskProgress; }
        set { this.SetProperty(ref this._taskProgress, value); }
    }
    /*public string TaskProgressString
    {
        get { return this._taskProgressString; }
        set { this.SetProperty(ref this._taskProgressString, value); }
    }*/

    [ObservableProperty]
    private string _taskProgressString;
    public string TaskResult
    {
        get { return this._taskResult; }
        set { this.SetProperty(ref this._taskResult, value); }
    }
    public Setup SetupSettings
    {
        get { return this.setupSettings; }
        set { this.SetProperty(ref this.setupSettings, value); }
    }

    public ObservableCollection<TaskFolder> MyOutlookTaskFolders
    {
        get { return this.taskfolder; }
        set { this.SetProperty(ref this.taskfolder, value); }
    }
    public TaskFolder SelectedTaskFolder
    {

        get { return this.selectedTaskFolder; }
        set
        {
            this.SetProperty(ref this.selectedTaskFolder, value);
            //handle your "event" here... 
            //h ttps://social.msdn.microsoft.com/Forums/sqlserver/en-US/c286f324-50fb-4641-a0d0-b36258de3847/uwp-xbind-event-handling-and-mvvm?forum=wpdevelop
            System.Diagnostics.Debug.WriteLine("Selected Item " + selectedTaskFolder.Name + " id " + selectedTaskFolder.Id);
            this.SetupSettings.ToDoTaskListID = selectedTaskFolder.Id;
            if (!string.IsNullOrEmpty(selectedTaskFolder.Id))
            {
                LoadTaskList();
            }
        }
    }

    public ObservableCollection<TaskResponse> MyOutlookTasks
    {
        get { return this.taskList; }
        set { this.SetProperty(ref this.taskList, value); }
    }
    public TaskResponse SelectedPurchaseTask
    {

        get { return this.selectedPurchaseTask; }
        set
        {
            this.SetProperty(ref this.selectedPurchaseTask, value);
            //handle your "event" here... 
            //System.Diagnostics.Debug.WriteLine("Selected Task " + selectedPurchaseTask.Subject+ " id " + selectedPurchaseTask.Id);
            if (this.selectedPurchaseTask != null)
                this.SetupSettings.ToDoTaskId = selectedPurchaseTask.Id;
        }
    }
    /// <summary>
    /// True, if can Save Settings 
    /// </summary>
    public bool CanExecute
    {
        get => this.canExecute;
        set => this.SetProperty(ref this.canExecute, value);
    }
    /// <summary>
    /// True, if ViewModel is busy, for Show Progress / Load Ring
    /// </summary>
    public bool IsBusy
    {
        get { return this._isBusy; }
        set { this.SetProperty(ref this._isBusy, value); }
    }
    public ICommand Submit { get; private set; }
    public ICommand LoadPicsCommand { get;}
    public ICommand GoToSamplePageCommand { get; }

    public RelayCommand SelectedTaskFolderChanged { get;private set; }
    #endregion

    #region OneDrive Properties

    private bool isSignedIn;
    public bool IsSignedIn
    {
        get => isSignedIn;
        set => SetProperty(ref isSignedIn, value);
    }

    private bool isBackedUp;
    public bool IsBackedUp
    {
        get => isBackedUp;
        set => SetProperty(ref isBackedUp, value);
    }
    public string MyToken { get; private set; }

    #endregion

    #region Constructor
    public SettingsVM_old() { }
    public SettingsVM_old(INavigator navigator, IMyService myservice, LoadImagesBackgroundworker backgroundWorkerService)
    {
        System.Diagnostics.Debug.WriteLine("Initialize SettingsViewModel ");
        _navigator = navigator;
        MessageService = myservice;
        _backgroundWorkerService = backgroundWorkerService;
        _backgroundWorkerService.StatusUpdated += OnStatusUpdated;
        _backgroundWorkerService.TaskCompleted += OnLoadPicturesTaskCompleted;
        _backgroundWorkerService.ProgressUpdated += OnLoadPicturesProgressUpdated;

        LoadPicsCommand = new AsyncRelayCommand(LoadPictureList);
        Submit = new RelayCommand(OnSaveSettings, () => true);
        NavigateToDesiredPage = new AsyncRelayCommand(OnNavigateToDesiredPage);
        SwitchToogled = new AsyncRelayCommand(SwitchToggled);
        TaskProgressString = " initial text progress";
        InitializeBackgroundWorker();
        LoadData();
    }
    #endregion

    /// <summary>
    /// Load Settings /Setup Data for the ViewModel
    /// </summary>
    public async Task LoadData()
    {
        AppConstants.LoadPictureListManually = false;
        CanExecute = false;
        IsBusy = true;
        try
        {
            await GetTokenOrSignIn();
            System.Diagnostics.Debug.WriteLine("Get Settings From Sqlite " + AppConstants.DatabasePath);
            SetupSettings = await DAL.AppDataBase.GetSetup();
            IList<TaskFolder> myfolderlist = await MessageService.GetTaskFolderFromGraph();
            taskfolder = myfolderlist.ToObservableCollection();
            selectedTaskFolder = myfolderlist.FirstOrDefault(t => t.Id == setupSettings.ToDoTaskListID);
            this.OnPropertyChanged("MyOutlookTaskFolders");
            this.OnPropertyChanged("SelectedTaskFolder");
            System.Diagnostics.Debug.WriteLine(AppConstants.DatabasePath);
            UpdateUI();

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Error " + ex.Message);
        }
        finally
        {
            IsBusy = false;
            CanExecute = true;
        }
    }

    #region Backgroundworker
    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }
    public string TaskStatus
    {
        get => _taskStatus;
        set
        {
            _taskStatus = value;
            OnPropertyChanged(nameof(TaskStatus));
        }
    }
    private void OnStatusUpdated(object sender, string status)
    {
        // Update the UI with the progress on the main thread
        TaskProgressString = status + " % geladen";
        if (status == "100")
        {
            _backgroundWorkerService.Stop();
            _taskResult = "Picture List Loaded";
        }
        UpdateUI();
        System.Diagnostics.Debug.WriteLine("Status : " + status + " % geladen");
    }
    private void OnLoadPicturesProgressUpdated(object sender, int taskProgress)
    {
        TaskProgress = taskProgress;
        System.Diagnostics.Debug.WriteLine("Progress : " + TaskProgress + " % geladen");
        //UpdateUI();
    }

    private void OnLoadPicturesTaskCompleted(object sender, string taskStatus)
    {
        _taskStatus = taskStatus;
        _taskResult = "List Loaded";
        System.Diagnostics.Debug.WriteLine("OnCompleted Picturesloaded , Result" + taskStatus);
        UpdateUI();
    }

    public void StartBackgroundTask()
    {
       _backgroundWorkerService.Start();
    }

    public void StopBackgroundTask()
    {
        _backgroundWorkerService.Stop();
    }

    private void InitializeBackgroundWorker()
    {
        // Initialize BackgroundWorker
    }

    #endregion

    #region DBServices



    #endregion

    #region graph services
    public async Task LogOutAsync()
    {
        await GraphService.SignOut();
        MyUsername = "";
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
            MyToken = mytoken;
            UpdateUI();
        }

        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
            return;
        }

    }
    #endregion

    public async Task LoadTaskList()
    {
        _isBusy = true;
        try
        {
            if (!string.IsNullOrEmpty(SelectedTaskFolder.Id))
            {
                IList<TaskResponse> tasksinFolder = await DAL.AppDataBase.GetTasksInFolder(SelectedTaskFolder.Id);
                System.Diagnostics.Debug.WriteLine("Must load tasks for folder : " + SelectedTaskFolder.Name);
                taskList = tasksinFolder.ToObservableCollection();

                if (taskList.Count() != 0)
                {
                    var ptask = tasksinFolder.FirstOrDefault(t => t.Id == SetupSettings.ToDoTaskId);
                    if (ptask != null)
                    {
                        selectedPurchaseTask = ptask;
                    }
                    else
                    {
                        selectedPurchaseTask = tasksinFolder.FirstOrDefault();
                    }
                    this.OnPropertyChanged("MyOutlookTasks");
                    this.OnPropertyChanged("SelectedPurchaseTask");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Exception in LoadTaskList : " + ex.Message);
        }
        finally
        {
            _isBusy = false;
        }
    }


 

    public async Task GetTokenOrSignIn()
    {
        try
        {
            var mytoken = await GraphService.GetTokenForUserAsync();
#if __ANDROID__
Android.Util.Log.Debug("YourAppName", "Access Token " + mytoken);
#else
            System.Diagnostics.Debug.WriteLine("Access Token " + mytoken);
#endif

            var myAuthResult = await GraphService.GetAuthResult();
            System.Diagnostics.Debug.WriteLine("Access Token " + myAuthResult.Account.Username);
            _myUsername = myAuthResult.Account.Username;
            if (!String.IsNullOrEmpty(_myUsername)) { IsSignedIn = true; } else { IsSignedIn = false; }
            UpdateUI();
        }

        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
            return;
        }
    }

    /// <summary>
    /// Loads Picturelist from Given OneDrive Folder and Save it in DataBase
    /// </summary>
    /// <returns></returns>
    private async Task LoadPictureList()
    {
        //IsBusy = true;
        //OnSaveSettings();
        AppConstants.LoadPictureListManually = true;
        _taskResult = "Loading Pictures ... ";
        _taskProgressString = "Initializing LoadPictureList ...";
        UpdateUI();
        // Start the background task when the page is loaded
        _backgroundWorkerService.StartImmediateTask();

    }
    #region Events / Functions



    /// <summary>
    /// Save Settings in Database if CanExecute = true
    /// </summary>
    private async void OnSaveSettings()
    {
        //TODO: Check if can save 
        //e.g. cant save if background tasks interval are smaller than 15 minutes 
        CanExecute = true;
        if (SetupSettings.OneDrivePictureFolder=="")
            CanExecute = false;
        IsBusy = true;
        if (CanExecute == false)
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
                CanExecute = true;
            }
        }
    }


    #endregion
    private async void UpdateUI()
    {
        // TODO: Nee Updatre UI ? 
        
        System.Diagnostics.Debug.WriteLine("UpdateUI");
            /*await DispatcherHelper.ExecuteOnUIThreadAsync(
                () =>
                {
                    OnPropertyChanged("MyUsername");
                    OnPropertyChanged("TaskResult");
                    OnPropertyChanged("TaskProgress");
                    OnPropertyChanged("TaskStatus");
                }
                , CoreDispatcherPriority.Normal);*/
    }
    private async Task OnNavigateToDesiredPage()
    {
        _ = _navigator?.NavigateBackAsync(this);
    }

    private async Task OnNavigateToSecondPage()
    {
        await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
    }

    private async Task SwitchToggled()
    {
        if (IsSignedIn == true) IsSignedIn = false;
        if (IsSignedIn == false) IsSignedIn = true;
        System.Diagnostics.Debug.WriteLine("Switch Toogle?");

    }
}
