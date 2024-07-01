namespace PiPic1.Controls;

public class InfoBoxViewModel : ObservableObject
{

    private InfoModel pim;
    public InfoModel PIM
    {
        get { return this.pim; }
        set { this.SetProperty(ref this.pim, value); }
    }

    private string totPics;
    public string TotPics
    {
        get { return this.totPics; }
        set { this.SetProperty(ref this.totPics, value); }
    }
}
