using FujiyReader.Core;
using PocketSharp.Models;
using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace FujiyReader.UWP.UI
{
    public class WebViewHtmlExtension
    {
        public static PocketItem GetHTML(DependencyObject obj)
        {
            return (PocketItem)obj.GetValue(HTMLProperty);
        }

        public static void SetHTML(DependencyObject obj, PocketItem value)
        {
            obj.SetValue(HTMLProperty, value);
        }

        // Using a DependencyProperty as the backing store for HTML.  This enables animation, styling, binding, etc... 
        public static readonly DependencyProperty HTMLProperty = DependencyProperty.RegisterAttached("HTML", typeof(PocketItem), typeof(WebViewHtmlExtension), new PropertyMetadata("", new PropertyChangedCallback(OnHTMLChanged)));

        private static async void OnHTMLChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebView wv = d as WebView;
            var item = (PocketItem)e.NewValue;
            if (wv != null && item != null)
            {
                string htmlContent = (await App.PocketClient.GetArticle(item, forceRefresh: false)).Content;

                //                < style >
                //    * {
                //                    max - width: 100 %;
                //                    overflow: auto;
                //                }
                //                img {
                //                    display: block;
                //                    max - width: 100 %;
                //                    height: auto;
                //                }
                //</ style >
                string content = @"<!DOCTYPE html>
<html>
<head>
<link rel=""stylesheet"" href=""ms-appx-web:///css/ui-" + Application.Current.RequestedTheme + @".css"" />
<style>
    html {
        overflow: initial;
        -ms-text-size-adjust: none;
        font-size: 3em;
    }
    body {
        margin: 0;
        width: 100%;
    }
    a { color: inherit; }
    img {
        max-width: 100% !important;
    }
    #body-wrapper {
        overflow-x: auto;
    }
</style>

<script>
  window.onscroll = GetScrollPosition;

    function GetScrollPosition() {
        window.external.notify(window.pageXOffset + "","" + window.pageYOffset);
    }

    function SetScrollPosition(X, Y) {
        window.scrollTo(X, Y);
        window.external.notify(window.pageXOffset + "","" + window.pageYOffset);
    }
</script>
</head>
<body>
    <div id=""body-wrapper"">
<h1>" + item.Title + @"</h1>
<a target=""_blank"" style=""text-decoration: none;"" href=" + item.Uri + ">" + item.Uri.Host + @"</a>
" + htmlContent + @"
<script type=""text/javascript"">for (var i = 0; i < document.links.length; i++) { document.links[i].onclick = function() { window.external.notify('LaunchLink:' + this.href); return false; } }</script>
    </div>
</body>
</html>
";
                //<script type=""text/javascript"">for (var i = 0; i < document.links.length; i++) { document.links[i].target = ""_blank"" }</script>
                wv.NavigateToString(content);
            }
        }

        public static async void WebView_NavigationCompleted(WebView sender, PocketItem item)
        {
            int verticalPosition = ArticleContentVerticalPosition.GetVerticalPosition(item);
            if (verticalPosition > 0)
            {
                await sender.InvokeScriptAsync("SetScrollPosition", new string[] { "0", verticalPosition.ToString() }).AsTask();
            }
        }

        public static async void WebView_ScriptNotify(PocketItem item, NotifyEventArgs e)
        {
            string data = e.Value;

            if (data.ToLower().StartsWith("launchlink:"))
            {
                try
                {
                    await Launcher.LaunchUriAsync(new Uri(data.Substring("launchlink:".Length), UriKind.Absolute));
                }
                catch (Exception)
                {
                    //TODO Could not build a proper Uri. Abandon.
                }
                return;
            }


            string[] Coordinates = data.Split(',');
            var x = double.Parse(Coordinates[0]);
            var y = (int)double.Parse(Coordinates[1]);

            ArticleContentVerticalPosition.SetVerticalPosition(item, y);
        }
    }
}
