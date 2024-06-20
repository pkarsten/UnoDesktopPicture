using System;
using System.Collections.Generic;
#if __ANDROID__
using Android.App;
#endif
using System.IO;
using System.Runtime.CompilerServices;

namespace PiPic1.Helpers;
public static class AppConstants
{
        public static string FilesReadWriteAppFolder = "Files.ReadWrite.AppFolder";

        public static string UserRead = "User.Read";

        public static string DeviceRead = "Device.Read";
        #region UI
        public const string APP_NAME = "Hello WIndows IOT";
        public const string ProductIdinStore = "";
        public const string SupportEmail = "pkarsten@live.de";
        public const string SupporterFirstName = "Peter";
        public static bool LoadPictureListManually { get; set; }
        #endregion
        public static string DatabaseName { get; } = "HelloPiUnoApp.Sqlite";
        public static Setup InitialSetupConfig { get; } = new Setup
        {
            Id = 1,
            EnableLogging = false,
            IntervalForDiashow = 10,
            IntervalForLoadPictures = 60,
            IntervalForLoadCalendarAndTasksInterval = 15,
            EnableCalendarAddon = false,
            EnableCalendarNextEvents = false,
            EnablePictureAddOn = false,
            EnableClock = true,
            EnablePurchaseTask = false,
            EnableTodayEvents = false,
            EventsOffset = +2,

        };
        public static PicFilter InitialPicFilterConfig { get; } = new PicFilter
        {
            Id = 1,
            CommonFolderQuery = "GroupByRating",
            VirtualFolder = ""
        };

        public const SQLite.SQLiteOpenFlags Flags =
        // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
        // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
        // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
#if __SKIA__ || HAS_UNO_SKIA_GTK || HAS_UNO_SKIA
            System.Diagnostics.Debug.WriteLine("GET SKIA  DB Path");
            return SaveFileInSkia();
#elif __ANDROID__
    System.Diagnostics.Debug.WriteLine("GET Android  DB Path");                
    return SaveFileInAndroid();
#elif !(NET6_0_OR_GREATER && WINDOWS)
    System.Diagnostics.Debug.WriteLine("GET Windows  DB Path");                    
    return SaveFileInWindows();
#elif WINDOWS
    System.Diagnostics.Debug.WriteLine("GET Windows 1 DB Path");                    
    return SaveFileInWindows();
#else
    System.Diagnostics.Debug.WriteLine("cant get Database Path, No Plattform Selected");    
    return "cant get Database Path, No Plattform Selected ";
#endif

        }
    }
        public static string SaveFileInSkia()
        {
        var basePath = AppContext.BaseDirectory; // Or a specific path you prefer
            var filePath = Path.Combine(basePath, AppConstants.DatabaseName);
            System.Diagnostics.Debug.WriteLine("SKIA  DB " + filePath);
            return filePath;
        }
        public static string SaveFileInWindows()
        {
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var filePath = Path.Combine(basePath, AppConstants.DatabaseName);
        System.Diagnostics.Debug.WriteLine("Win DB " + filePath);
        return filePath;
        }
        public static string SaveFileInAndroid()
        {

#if __ANDROID__
            var context = Android.App.Application.Context;
            var path = context.GetExternalFilesDir(null).AbsolutePath;
            var filePath = Path.Combine(path, AppConstants.DatabaseName);
            System.Diagnostics.Debug.WriteLine("Android  DB" + filePath);
            return filePath;
#else
        return null;
#endif
        }

}
