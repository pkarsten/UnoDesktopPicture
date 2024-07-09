using System;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Imaging;
using PiPic1.Helpers;
using PiPic1.Services;

namespace PiPic1.Presentation;

public partial class KioskViewModel : ObservableObject
{
    #region Fields and Properties
    private static DispatcherTimer? pictimer;
    private INavigator _navigator;
    protected IMyService MessageService { get; }
    private BitmapImage? _dashImage;
    private string? _pictureName;
    public ICommand GoToSettings { get; }
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

    private double _myoffset;
    private DateTime _currentTime = DateTime.UtcNow;
    public DateTime CurrentTime
    {

        get
        {
            DateTime mydt = DateTime.UtcNow.AddHours(_myoffset);
            return mydt;
        }
    }
    private bool _enableClock;
    public bool EnableClock
    {
        get { return this._enableClock; }
        set { this.SetProperty(ref this._enableClock, value); }
    }
    private DispatcherTimer _dtimer;
    private ObservableCollection<CalendarEvent> todayEvents = new ObservableCollection<CalendarEvent>();
    private ObservableCollection<CalendarEvent> nextcalendarEvents = new ObservableCollection<CalendarEvent>();
    private ObservableCollection<ToDoTask> todotasks = new ObservableCollection<ToDoTask>();
    private bool hideTodayEvents;
    public bool HideTodayEvents
    {
        get { return this.hideTodayEvents; }
        set
        {
            this.SetProperty(ref this.hideTodayEvents, value);
            System.Diagnostics.Debug.WriteLine("Hide today " + value + " ~ " + this.hideTodayEvents);
        }
    }

    public ObservableCollection<CalendarEvent> NextCalendarEvents
    {
        get { return this.nextcalendarEvents; }
        set { this.SetProperty(ref this.nextcalendarEvents, value); }
    }
    public ObservableCollection<CalendarEvent> TodayCalendarEvents
    {
        get { return this.todayEvents; }
        set { this.SetProperty(ref this.todayEvents, value); }
    }
    public ObservableCollection<ToDoTask> ToDoTasks
    {
        get { return this.todotasks; }
        set
        {
            this.SetProperty(ref this.todotasks, value);
        }
    }

    public InfoModel InfoM
    {
        get { return this._infoM; }
        set { this.SetProperty(ref this._infoM, value); }
    }
    public string ToDoTaskContent
    {
        get { return this.purchtaskcontent; }
        set { this.SetProperty(ref this.purchtaskcontent, value); }
    }
    public string ToDoTaskSubject
    {
        get { return this.purchtasksubject; }
        set { this.SetProperty(ref this.purchtasksubject, value); }
    }
    private InfoModel _infoM = new InfoModel();
    private string purchtaskcontent = "";
    private string purchtasksubject = "";
    private string tasklisttitel;
    public string TaskListTitel
    {
        get { return this.tasklisttitel; }
        set { this.SetProperty(ref this.tasklisttitel, value); }
    }

    private bool showTasks;
    public bool ShowTasks
    {
        get { return showTasks; }
        set
        {
            this.SetProperty(ref this.showTasks, value);
        }
    }

    private bool showTodayEvents;
    public bool ShowTodayEvents
    {
        get { return showTodayEvents; }
        set
        {
            this.SetProperty(ref this.showTodayEvents, value);
        }
    }
    private bool showNextEvents;
    public bool ShowNextEvents
    {
        get { return showNextEvents; }
        set
        {
            this.SetProperty(ref this.showNextEvents, value);
        }
    }
    private bool showCalendarAddOn;
    public bool ShowCalendarAddOn
    {
        get { return showCalendarAddOn; }
        set
        {
            this.SetProperty(ref this.showCalendarAddOn, value);
        }
    }

    private readonly LoadGraphDataBackgroundworker _graphDataWorkerService;
    private readonly LoadImagesBackgroundworker _imagesWorkerService;

    #endregion

    #region Constructor
    public KioskViewModel(INavigator navigator, IMyService myservice, LoadGraphDataBackgroundworker backgroundWorkerService, LoadImagesBackgroundworker imgService)
    {
        _navigator = navigator;
        MessageService = myservice;
        GoToSettings = new AsyncRelayCommand(OnNavigateToSettingsPage);
        var dispatcherQueue = (App.Current as App).DispatcherQueue;
        if (dispatcherQueue == null)
        {
            throw new InvalidOperationException("DispatcherQueue is null. Ensure this method is called from the UI thread.");
        }
        dispatcherQueue.TryEnqueue(() =>
        {
            LoadData();
        });
        _graphDataWorkerService = backgroundWorkerService;
        _graphDataWorkerService.StatusUpdated += OnStatusUpdated;
        _graphDataWorkerService.TaskCompleted += LoadGraphDataTaskCompleted;

        _imagesWorkerService = imgService;
        _imagesWorkerService.StatusUpdated += OnImgWorkerStatusUpdated;
        _imagesWorkerService.TaskCompleted += OnImgWorkerTaskCompleted;
    }
    #endregion
    /// <summary>
    /// Load Initial Settings /Setup Data for the ViewModel
    /// </summary>
    public async Task LoadData()
    {
        try
        {
            await this.UpdateDashBoardImageAsync();
            await LoadCalendarEvents();
            await LoadPurchTask();
            await LoadDatabaseInfos();
            await StartBackgroundTask();
            var s = await DAL.AppDataBase.GetSetup();
            if (s != null)
            {
                _myoffset = s.EventsOffset;
            }
            else
            {
                _myoffset = AppConstants.InitialSetupConfig.EventsOffset;
            }
            await CheckClockStatus(s);

            if (s.IntervalForDiashow < 30) { s.IntervalForDiashow = 30; };
            StartPicChangeTimer(0, s.IntervalForDiashow, async () => await this.UpdateDashBoardImageAsync());
            ShowTasks = !s.EnablePurchaseTask;
            ShowTodayEvents = !s.EnableTodayEvents;
            ShowNextEvents = !s.EnableCalendarNextEvents;
            ShowCalendarAddOn = !s.EnableCalendarAddon;
           
            UpdateUI();
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    #region Clock
    private async Task CheckClockStatus(Setup setup)
    {
        if (setup.EnableClock == true)
        {
            _dtimer = new DispatcherTimer(); //For Clock 
            await StartClock();
        }
        else
        {
            await StopClock();
        }
    }

    private async Task StartClock()
    {
        EnableClock = true;
        _dtimer.Tick += Timer_Tick;
        _dtimer.Interval = new TimeSpan(0, 0, 1);
        _dtimer.Start();
    }
    private async Task StopClock()
    {
        EnableClock = false;
        _dtimer.Stop();
    }

    /// <summary>
    /// Change Time on CLock if Clock COntrol is Enable
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer_Tick(object sender, object e)
    {
        this.OnPropertyChanged("CurrentTime");
    }
    #endregion

    #region Timer
    /// <summary>
    /// Starts a timer to perform the specified action at the specified interval.
    /// </summary>
    /// <param name="intervalInMinutes">The interval.</param>
    /// <param name="action">The action.</param>
    public static void StartPicChangeTimer(int intervalInMinutes, int intervalInSeconds, Action action)
    {
        if(pictimer != null)
        {
            pictimer.Stop();
        }
        pictimer = new DispatcherTimer();
        pictimer.Interval = new TimeSpan(0, intervalInMinutes, intervalInSeconds);
        pictimer.Tick += (s, e) => action();
        pictimer.Start();
    }

    private static async Task StopPicChangeTimer()
    {
        pictimer.Stop();
    }
    #endregion

    #region Calendar Events
    private async Task LoadCalendarEvents()
    {
        var t = DAL.AppDataBase.GetNextEvents().ToObservableCollection();
        if (t != null)
        {
            nextcalendarEvents = t;
        }

        if (t == null)
        {
            nextcalendarEvents = null;
        }

        var t1 = DAL.AppDataBase.GetTodayEvents().ToObservableCollection();
        if (t1.Count > 0)
        {
            todayEvents = t1;
            hideTodayEvents = false;
            HideTodayEvents = false;
        }
        else
        {
            todayEvents = null;
            hideTodayEvents = true;
            HideTodayEvents = true;
        }
    }
    #endregion

    #region ToDoTasks
    private async Task LoadPurchTask()
    {
        var s = DAL.AppDataBase.GetSetup().Result;
        tasklisttitel = s.ToDoTaskListName;

        var pt = DAL.AppDataBase.GetToDoTasks().ToObservableCollection(); ;
        if (pt != null)
        {
            todotasks = pt;
        }
    }
    #endregion

    #region DataBaseInfos
    private async Task LoadDatabaseInfos()
    {
        await DAL.AppDataBase.SaveLogEntry(LogType.AppInfo, "Load Database Infos");
        _infoM.TotalPicsinDB = "Total Bilder: " + await DAL.AppDataBase.CountPicsInTable();
        _infoM.ViewedPics = "Bereits angezeigt: " + await DAL.AppDataBase.CountPicsInTable(true);
        _infoM.NonViewedPics = "Fehlen noch: " + await DAL.AppDataBase.CountPicsInTable(false);
        UpdateUI();
    }
    #endregion

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
            UpdateUI();
        }

        try
        {
            await LoadDatabaseInfos();
        }
        catch (Exception ex) { }
        finally
        {
            UpdateUI();
        }
    }

    private async void UpdateUI()
    {
        var dispatcherQueue = (App.Current as App).DispatcherQueue;
        if (dispatcherQueue == null)
        {
            throw new InvalidOperationException("DispatcherQueue is null. Ensure this method is called from the UI thread.");
        }
        dispatcherQueue.TryEnqueue(() => {
            OnPropertyChanged("DashImage");
            OnPropertyChanged("DashImageDescription");
            OnPropertyChanged("CurrentTime");
            OnPropertyChanged("TodayCalendarEvents");
            OnPropertyChanged("NextCalendarEvents");
            OnPropertyChanged("HideTodayEvents");
            OnPropertyChanged("Friends");
            OnPropertyChanged("ToDoTasks");
            OnPropertyChanged("ToDoTaskContent");
            OnPropertyChanged("TaskListTitel");
            OnPropertyChanged("ToDoTaskSubject");
            OnPropertyChanged("EnableCLock");
            OnPropertyChanged("InfoM");
            OnPropertyChanged("HasDescription");
            OnPropertyChanged("ShowTasks");
        });
    }

    private async Task OnNavigateToSettingsPage()
    {
        await StopClock();
        await StopPicChangeTimer();
        _graphDataWorkerService.Stop();
        _imagesWorkerService.Stop();
        await _navigator.NavigateRouteForResultAsync(this, "Settings", "");
    }

    #region Backgroundworker
    private string? _taskResult;
    public async Task StartBackgroundTask()
    {
        _graphDataWorkerService.Start();
        _imagesWorkerService.Start();
    }

    public async Task CancelBackgroundTask()
    {
        _graphDataWorkerService.Stop();
        _imagesWorkerService.Stop();
    }
    private async void OnStatusUpdated(object sender, string status)
    {
        //TaskProgressString = status + " % geladen";//Needed for show progress on UI
        if (status == "100")
        {
            //_backgroundWorkerService.Stop();
            _taskResult = "Calendar Events and Tasks Loaded";
            await LoadCalendarEvents();
            await LoadPurchTask();
        }
        UpdateUI();
        System.Diagnostics.Debug.WriteLine("Load Graph Data Status : " + status + " % geladen");
    }

    private async void LoadGraphDataTaskCompleted(object sender, string taskStatus)
    {
        System.Diagnostics.Debug.WriteLine("LoadGraphDataTaskCompleted , Result" + taskStatus);
        UpdateUI();
    }

    private async void OnImgWorkerStatusUpdated(object sender, string status)
    {
        //TaskProgressString = status + " % geladen";//Needed for show progress on UI
        if (status == "100")
        {
            //_backgroundWorkerService.Stop();
            _taskResult = "New Image List Loaded";
        }
        UpdateUI();
        System.Diagnostics.Debug.WriteLine("OnImgWorkerStatusUpdated Status : " + status + " % geladen");
    }

    private async void OnImgWorkerTaskCompleted(object sender, string taskStatus)
    {
        System.Diagnostics.Debug.WriteLine("Image List Loaded Completed , Result" + taskStatus);
        UpdateUI();
    }
    #endregion
}
