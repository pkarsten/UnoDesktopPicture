using Uno.UI.Runtime.Skia;


namespace PiPic1;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // equals SKIA.GTK host.RenderSurfaceType = RenderSurfaceType.Software? 
        // is needed if no images are displayed in the xaml page on the raspberry
        FeatureConfiguration.Rendering.UseOpenGLOnX11 = false;

        var host = SkiaHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWindows()
            .Build();
        host.Run();
    }
}
