using MSGraph.Response;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using MSGraph;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PiPic1.Services;

public class MyService : IMyService
{
    public string GetMessage()
    {
        return "Hello from Message Service & Dependency Injection";
    }
    
    public async Task<bool> LoadImageListFromOneDrive()
    {
        try
        {
            Exception error = null;
            ItemInfoResponse folder = null;
            IList<ItemInfoResponse> children = null;
            int _progress = 0;

            //// Initialize Graph client
            var accessToken = await GraphService.GetTokenForUserAsync();
            var graphService = new GraphService(accessToken);

            try
            {
                var s = await DAL.AppDataBase.GetSetup();
                //TODO: folder = await graphService.GetPhotosAndImagesFromFolder(s.OneDrivePictureFolder);
                folder = await graphService.GetPhotosAndImagesFromFolder("/Familienbereich/Bilder");
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
        return true;
    }

    // The background task activity.
    //
    public async Task<DashBoardImage> StreamImageFromOneDrive()
    {
        try
        {
            DashBoardImage dbi = new DashBoardImage();
            BitmapImage bitmapimage = new BitmapImage();
            if (await DAL.AppDataBase.CountAllPictures() == 0)
            {
                // Load Image List from Onedrive ? 
            }
            else
            {
                // Get Random ItemInfoResponse from Table 
                var item = DAL.AppDataBase.GetRandomInfoItemResponse();



                // Only load a detail view image for image items. Initialize the bitmap from the image content stream.
                Exception error = null;
                ItemInfoResponse foundFile = null;
                Stream contentStream = null;

                //// Initialize Graph client
                var accessToken = await GraphService.GetTokenForUserAsync();
                var graphService = new GraphService(accessToken);
                try
                {
                    //TODO: Handle if item is null -> DB eror
                    foundFile = await graphService.GetItem(item.OneDriveId);

                    if (foundFile == null)
                    {
                        await DAL.AppDataBase.SaveLogEntry(LogType.Error, $"Image Not found Id: {item.OneDriveId}");
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("Found Image: " + item.Name + " Id: " + item.OneDriveId + item.DownloadUrl);

                    }

                    // Get the file's content
                    contentStream = await graphService.RefreshAndDownloadContent(foundFile, false);

                    if (contentStream == null)
                    {
                        await DAL.AppDataBase.SaveLogEntry(LogType.Error, $"Content Stream not found: {foundFile.Name}");
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                    await DAL.AppDataBase.SaveLogEntry(LogType.Error, error.Message);
                    DAL.AppDataBase.DeletePicture(item);
                    return null;
                }

                // Save the retrieved stream 
                var memoryStream = contentStream as MemoryStream;

                if (memoryStream != null)
                {
                    await bitmapimage.SetSourceAsync(memoryStream.AsRandomAccessStream());

                }
                else
                {
                    using (memoryStream = new MemoryStream())
                    {
                        await contentStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;
                        await bitmapimage.SetSourceAsync(memoryStream.AsRandomAccessStream());
                    }
                }

                item.Viewed = true;
                //dbi.Description = item.Description;
                dbi.Description = item.Name + " " + DateTime.Now;
                await DAL.AppDataBase.SavePicture(item);
            }
            dbi.Photo = bitmapimage;
            return dbi;
        }
        catch (Exception ex)
        {
            await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in StreamImageFromOneDrive(): " + ex.Message);
            return null;
        }
        finally
        {
            await DAL.AppDataBase.SaveLogEntry(LogType.Info, "Dashboard Picture Changed at: " + DateTime.UtcNow);
            DAL.AppDataBase.CheckForViewedPictures();
        }

    }
}
