using System.Collections.ObjectModel;
using MSGraph.Response;
namespace PiPic1.Services;

public interface IMyService
{
    string GetMessage();

    Task<bool> LoadImageListFromOneDrive();

    Task SimulateBackWord();
    Task<DashBoardImage> StreamImageFromOneDrive();

    Task<IList<TaskFolder>> GetTaskFolderFromGraph();
   
    Task LoadCalendarEventsAndTasks();
}
