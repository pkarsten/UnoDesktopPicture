using System.ComponentModel;
using System.Timers;
namespace PiPic1.Services;

public class LoadImagesService : ILoadImagesServices
{
    private BackgroundWorker _backgroundWorker;
    private System.Timers.Timer _timer;

    public LoadImagesService()
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
        _timer = new System.Timers.Timer(1000); // Set interval to 1 seconds
        _timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        StartImmediateTask();
    }

    private void BackgroundWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void BackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
    {
        throw new NotImplementedException();
    }

    public void StartImmediateTask()
    {
        if (!_backgroundWorker.IsBusy)
        {
            _backgroundWorker.RunWorkerAsync();
        }
    }
}
