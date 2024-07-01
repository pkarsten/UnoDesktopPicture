using Newtonsoft.Json;
using System.Collections.ObjectModel;


namespace MSGraph.Response;

// Task Folder: https://graph.microsoft.com/beta/me/outlook/taskFolders
/// <summary>
/// MS Graph Outlook TaskFolder 
/// </summary>
public class TaskFolder
{
    [JsonProperty("id")]
    public string Id
    {
        get;
        set;
    }

    [JsonProperty("name")]
    public string Name
    {
        get;
        set;
    }
}

public class ParseTaskFolderResponse
{
    public ObservableCollection<TaskFolder> Value
    {
        get;
        set;
    }
    [JsonProperty("@odata.nextLink")]
    public string NextLink
    {
        get;
        set;
    }
}
