using MSGraph.Response;
using Windows.ApplicationModel.Core;
using System.Collections.ObjectModel;
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

    public async Task<IList<TaskFolder>> GetTaskFolderFromGraph()
    {

        Exception error = null;
        IList<TaskFolder> folders = null;

        //// Initialize Graph client
        var accessToken = await GraphService.GetTokenForUserAsync();
        var graphService = new GraphService(accessToken);

        try
        {
            folders = await graphService.GeTaskFolders();
            foreach (TaskFolder f in folders)
            {
                System.Diagnostics.Debug.WriteLine("Name: " + f.Name + " - Id: " + f.Id);
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine(LogType.Error, "Error in GetTaskFolderFromGraph " + error.Message);
            }
        }

        return folders;
    }

    public async Task<ObservableCollection<TaskFolder>> GetTaskFolderFromGraph1()
    {
        Exception error = null;
        IList<TaskFolder> folders = null;

        //// Initialize Graph client
        var accessToken = await GraphService.GetTokenForUserAsync();
        var graphService = new GraphService(accessToken);

        try
        {
            folders = await graphService.GeTaskFolders();
            foreach (TaskFolder f in folders)
            {
                System.Diagnostics.Debug.WriteLine("Name: " + f.Name + " - Id: " + f.Id);
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            if (error != null)
            {
                System.Diagnostics.Debug.WriteLine(LogType.Error, "Error in GetTaskFolderFromGraph " + error.Message);
            }
        }

        return folders.ToObservableCollection();
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
                dbi.Description = item.Description;
                //dbi.Description = item.Name + " " + DateTime.Now;
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

    public async Task SimulateBackWord()
    {
        int _progress = 0;
        for (int i = 0; i < 100; i++)
        {
            _progress = i;
            Thread.Sleep(1000);
            System.Diagnostics.Debug.WriteLine("Progress in Back Worker " + _progress);
        }
    }

    public async Task LoadCalendarEventsAndTasks()
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
                        //BGTasksSettings.NextEvents = nextevents.ToObservableCollection();
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
                await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in LoadImageListFromOneDrive() " + ex.Message);
            }
        }
        catch (Exception ex)
        {
            await DAL.AppDataBase.SaveLogEntry(LogType.Error, "Exception  in LoadImageListFromOneDrive() " + ex.Message);
        }
        finally
        {
        }
    }
}
