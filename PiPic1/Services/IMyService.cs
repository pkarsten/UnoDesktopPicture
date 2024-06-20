namespace PiPic1.Services;

public interface IMyService
{
    string GetMessage();

    Task<bool> LoadImageListFromOneDrive();
    Task<DashBoardImage> StreamImageFromOneDrive();
}
