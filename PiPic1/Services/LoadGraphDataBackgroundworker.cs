using MSGraph;
using MSGraph.Response;
using System.ComponentModel;
using System.Timers;
using PiPic1.Helpers;

namespace PiPic1.Services;
public sealed class LoadGraphDataBackgroundworker
{
    #region Fields
    private System.Timers.Timer _timer;
    private readonly BackgroundWorker _backgroundWorker;
    private bool _isRunning;
    int _progress = 0;
    volatile bool _cancelRequested = false;
    #endregion

    #region Properties
    public event EventHandler<string> StatusUpdated;
    public event EventHandler<string> TaskResult;
    public event EventHandler<string> TaskCompleted;
    public event EventHandler<int> ProgressUpdated;
    public Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue;

    #endregion

    #region Constructor
    public LoadGraphDataBackgroundworker()
    {
        // Initialize BackgroundWorker
        _backgroundWorker = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        _backgroundWorker.DoWork += BackgroundWorker_DoWork;
        _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;

        InitializeTimer();
    }
    #endregion

    #region Events
    private async void InitializeTimer()
    {
        // Initialize Timer
        //_timer = new System.Timers.Timer(5000); // Set interval to 5 seconds, 60 seconds = 60000
        var minutesinterval = await GetIntervalForTimer();
        _timer = new System.Timers.Timer(minutesinterval * 60000); 
        _timer.Elapsed += Timer_Elapsed;
    }
    
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        StartImmediateTask();
    }

    public void Start()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _timer.Start();
        }
    }
    public bool IsRunning()
    {
        return _isRunning;
    }

    public void Stop()
    {
        if (_isRunning)
        {
            _isRunning = false;
            _timer.Stop();

            if (_backgroundWorker.IsBusy)
            {
                _backgroundWorker.CancelAsync();
            }
        }
        _cancelRequested = true;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _backgroundWorker?.Dispose();
    }

    public void StartImmediateTask()
    {
        if (!_backgroundWorker.IsBusy)
        {
            _backgroundWorker.RunWorkerAsync();
        }
    }
    #endregion

    #region BackgroundEvents
    private async void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        /*int _progress = 0;
        for (int i = 0; i < 100; i++)
        {
            _progress = i;
            // Marshal the call back to the main UI thread
            Thread.Sleep(100);
            StatusUpdated?.Invoke(this, _progress.ToString());
            System.Diagnostics.Debug.WriteLine("Progress in Back Worker " + _progress);
        }*/
        await LoadCalendarEventsAndTasks();
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

    #endregion

    #region Hardwork
    //
    // The background task activity.
    //
    private async Task LoadCalendarEventsAndTasks()
    {
        try
        {
            //// Initialize Graph client
            var accessToken = await GraphService.GetTokenForUserAsync();
            var graphService = new GraphService(accessToken);

            try
            {
                var s = await DAL.AppDataBase.GetSetup();
                if (s.EnableCalendarAddon)
                {
                    await DAL.AppDataBase.DeleteAllCalendarEvents();
                    //
                    // Graphservice for get Calendar Events
                    //

                    if (s.EnableCalendarNextEvents)
                    {
                        IList<CalendarEventItem> nextevents = await graphService.GetCalendarEvents(s.NextEventDays);
                        foreach (var o in nextevents)
                        {
                            var ce = new CalendarEvent();
                            //TODO: It seems that sqliteDB all the time saves datetime  as UTC , 
                            //Perhaps add language localisation settings ? 
                            // no way to save it to localtime or other Format? So add here 4 Houts for my timezone 
                            //Winter Time in Düsseldorf Add 1, Summer Time Add 2 , or viceversa? 
                            ce.StartDateTime = o.StartDateTime.dateTime.AddHours(s.EventsOffset);
                            //TODO: Summertime ? Get here UTC Date ? TODO: Time zone in setup choose? 
                            ce.Subject = o.Subject;
                            if (ce.StartDateTime.Day == DateTime.UtcNow.Day)
                                ce.TodayEvent = true;
                            else
                                ce.TodayEvent = false;

                            ce.IsAllDay = o.IsAllDay;

                            // TODO: more test for this here, perhaps there are Events that we would see , then don't ignore them 
                            // Problem is when StartTime is between 0:00-02:00 , example: exists an IsAllDay Event on 10.04.19 LocalTime (Begins 0:00, Ends at 11.04.19 0:00)
                            // when the day changes (Localtime) on 0:00 Uhr, then it will list this event as Today Event (because it ends on 11.04) ...
                            if (ce.StartDateTime.Day + 1 == DateTime.UtcNow.Day)
                            {
                                if (ce.IsAllDay)
                                {
                                    ce.IgnoreEvent = true;
                                }
                            }

                            //ce.StartDateTime.ToLocalTime();
                            //string us = String.Format("{0:yyyy-MM-ddTHH:mm:ss}", o.StartDateTime.dateTime);
                            //System.Diagnostics.Debug.WriteLine("UTC Time: " + us + " " + o.Subject);
                            //DateTime locDT = o.StartDateTime.dateTime.ToLocalTime();
                            //string strutcStart = String.Format("{0:yyyy-MM-ddTHH:mm:ss}", locDT);
                            //System.Diagnostics.Debug.WriteLine("Loc Time: " + strutcStart + " " + o.Subject);
                            //ce.StartDateTime = new DateTime(locDT.Year,locDT.Month,locDT.Day, locDT.Hour,locDT.Minute,locDT.Second);
                            //ce.StartDateTime = new DateTime(2019, 4, 16, 22, 22, 22);

                            await DAL.AppDataBase.SaveCalendarEvent(ce);
                        }
                    }

                }

                if (s.EnablePurchaseTask)
                {
                    await DAL.AppDataBase.DeleteToDoTask();
                    //Graph Service for get Tasks
                    var mypurchtask = await graphService.GetTasksFromToDoTaskList(s.ToDoTaskListID);

                    if (mypurchtask != null)
                    {
                        foreach (TaskResponse p in mypurchtask)
                        {
                            var pt = new ToDoTask();
                            pt.Subject = p.Subject;
                            pt.BodyText = p.TaskBody.Content;
                            await DAL.AppDataBase.SaveToDoTask(pt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in LoadCalendarEventsAndTasks() " + ex.Message);
            }
            _progress = 100;
            StatusUpdated?.Invoke(this, _progress.ToString());
        }
        catch (Exception ex)
        {
            await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in LoadCalendarEventsAndTasks() " + ex.Message);
        }
        finally
        {
        }
    }
    #endregion

    #region Functions
    private async Task<double> GetIntervalForTimer()
    {
        var s = await DAL.AppDataBase.GetSetup();
        if (s != null)
        {
            return s.IntervalForLoadCalendarAndTasksInterval;
        }
        else { return AppConstants.InitialSetupConfig.IntervalForLoadCalendarAndTasksInterval; }
    }
    #endregion
}
