using CefSharp;
using CefSharp.Wpf;

public class CustomLifeSpanHandler : ILifeSpanHandler
{
    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;
    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }
    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

    public bool OnBeforePopup(
        IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
        string targetUrl, string targetFrameName,
        WindowOpenDisposition targetDisposition, bool userGesture,
        IPopupFeatures popupFeatures, IWindowInfo windowInfo,
        IBrowserSettings browserSettings, ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        // 阻止弹窗，直接在当前页面跳转到新URL
        chromiumWebBrowser.Load(targetUrl);
        newBrowser = null;
        return true; // true = 阻止弹窗
    }
}