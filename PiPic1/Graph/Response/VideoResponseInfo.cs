using Newtonsoft.Json;

namespace MSGraph.Response;

public class VideoResponseInfo
{
    public int Bitrate
    {
        get;
        set;
    }

    [JsonProperty("duration")]
    public int DurationMilliSeconds
    {
        get;
        set;
    }

    public int Height
    {
        get;
        set;
    }

    public int Width
    {
        get;
        set;
    }
}
