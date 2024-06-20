using SQLite;
using System;
using System.Reflection;
using Windows.Storage;

namespace PiPic1.Models;
/// <summary>
/// Represents a Rated Picture 
/// </summary>
public sealed class FavoritePic
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Gets or Sets the Number of Rated Stars
        /// </summary>
        public int Stars { get; set; }

        public string Name { get; set; }

        public string LibraryPath { get; set; }

        public bool Viewed { get; set; }

        public bool DownloadedFromOneDrive { get; set; }

        public string OneDriveId { get; set; }

        public string DownloadUrl { get; set; }

        public string Status { get; set; }

        public string Description { get; set; }
        public string Tags { get; set; }
    }

    public enum PicStatus
    {
        UpToDate, Indefinable
    }

