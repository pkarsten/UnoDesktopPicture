using MSGraph;
using MSGraph.Response;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Microsoft.UI.Dispatching;
using Microsoft.Graph.Models;
using Windows.ApplicationModel.Background;
using Windows.System;

namespace PiPic1.Services;

public sealed class LoadImagesBackgroundworker


{
    #region Fields
    private readonly System.Timers.Timer _timer;
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
    public LoadImagesBackgroundworker()
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

        // Initialize Timer
        //_timer = new Timer(5000); // Set interval to 5 seconds
        _timer = new System.Timers.Timer(5000); // Set interval to 1 seconds
        _timer.Elapsed += Timer_Elapsed;
    }
    #endregion

    #region Events
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
            StartImmediateTask(); // Start the task immediately
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
        await LoadImageListFromOneDrive();
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
    private async Task LoadImageListFromOneDrive()
    {
        if ((_cancelRequested == false) && (_progress < 100))
        {
        }
        try
        {
            Exception error = null;
            ItemInfoResponse folder = null;
            IList<ItemInfoResponse> children = null;

            //// Initialize Graph client
            var accessToken = await GraphService.GetTokenForUserAsync();
            var graphService = new GraphService(accessToken);

            try
            {
                var s = await DAL.AppDataBase.GetSetup();
                folder = await graphService.GetPhotosAndImagesFromFolder(s.OneDrivePictureFolder);
                children = await graphService.PopulateChildren(folder);

                if (children != null)
                {

                    try
                    {

                        //https://gunnarpeipman.com/csharp/foreach/
                        ///TODO: Null Exception here when Children is null 
                        int xyz = 0;
                        foreach (ItemInfoResponse iir in children.ToList())
                        {
                            if (iir.Image != null)
                            {
                                //System.Diagnostics.Debug.WriteLine("PhotoName : " +xyz+ " - "  + iir.Name + "Id: " + iir.Id);
                                xyz += 1;
                                //iri = iir;
                            }
                            else
                            {
                                children.Remove(iir);
                            }
                        }
                        xyz = 0;
                        int totalFiles = children.Count;
                        int filesProcessed = 0;
                        //                            await HelloWindowsIotDataBase.DeleteAllPictures();
                        foreach (var iri in children)
                        {
                            if (iri.Image != null)
                            {

                                filesProcessed++;
                                _progress = (int)((double)filesProcessed / totalFiles * 100);
                                StatusUpdated?.Invoke(this, _progress.ToString());
                                var dbPic = DAL.AppDataBase.GetPictureByOneDriveId(iri.Id);
                                if (dbPic == null)
                                {
                                    var fp = new FavoritePic();
                                    fp.DownloadedFromOneDrive = true;
                                    fp.Viewed = false;
                                    fp.Name = iri.Name;
                                    fp.DownloadUrl = iri.DownloadUrl;
                                    fp.Name = iri.Name;
                                    fp.Description = iri.Description;
                                    fp.OneDriveId = iri.Id;
                                    fp.Status = "UpToDate";
                                    System.Diagnostics.Debug.WriteLine("New Pic in DB : " + xyz + " - " + iri.Name + "Id: " + iri.Id);
                                    await DAL.AppDataBase.SavePicture(fp);
                                }
                                else
                                {
                                    var fp = dbPic;
                                    fp.Status = "UpToDate";
                                    fp.Description = iri.Description;
                                    System.Diagnostics.Debug.WriteLine("Pic Update in DB PhotoName : " + xyz + " - " + iri.Name + "Id: " + iri.Id + "Desc: " + iri.Description);
                                    await DAL.AppDataBase.SavePicture(fp);
                                }
                                xyz += 1;

                            }

                        }

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception in LoadImageListFromOneDrive: " + ex.Message);
                        await DAL.AppDataBase.SaveLogEntry(LogType.Error, ex.Message);
                    }
                    finally
                    {
                        DAL.AppDataBase.DelIndefinablePics();
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            _progress = 100;
        }
        catch (Exception ex)
        {
            await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in LoadImageListFromOneDrive() " + ex.Message);
        }
        finally
        {

        }
    }
    #endregion
}
