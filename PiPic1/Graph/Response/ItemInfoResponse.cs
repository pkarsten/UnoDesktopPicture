using Newtonsoft.Json;

namespace MSGraph.Response;

public class ItemInfoResponse : DriveItem
{
    private FileResponseInfo _file;
    private FolderResponseInfo _folder;

    public AudioResponseInfo Audio
    {
        get;
        set;
    }

    //[JsonProperty("@content.downloadUrl")]
    [JsonProperty("@microsoft.graph.downloadUrl")]
    public string DownloadUrl
    {
        get;
        set;
    }

    private string _name;
    public string Name { get { return _name; } set { _name = value; } }
    private string _description;
    public string Description { get { return _description; } set { _description = value; } }

    public FileResponseInfo File

    {
        get
        {
            return _file;
        }
        set
        {
            _file = value;

            if (value != null)
            {
                Kind = FileFolderKind.File;
            }
        }
    }

    public FolderResponseInfo Folder
    {
        get
        {
            return _folder;
        }
        set
        {
            _folder = value;

            if (value != null)
            {
                Kind = FileFolderKind.Folder;
            }
        }
    }

    public ImageResponseInfo Image
    {
        get;
        set;
    }

    public ParentReferenceInfo ParentReference
    {
        get;
        set;
    }

    public PhotoResponseInfo Photo
    {
        get;
        set;
    }

    public VideoResponseInfo Video
    {
        get;
        set;
    }

    public class ParentReferenceInfo
    {
        public string DriveId
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }
    }
}
