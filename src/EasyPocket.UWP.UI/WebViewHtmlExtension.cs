﻿using EasyPocket.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace EasyPocket.UWP.UI
{
    public class WebViewHtmlExtension
    {
        public static PocketItemWithContent GetHTML(DependencyObject obj)
        {
            return (PocketItemWithContent)obj.GetValue(HTMLProperty);
        }

        public static void SetHTML(DependencyObject obj, PocketItemWithContent value)
        {
            obj.SetValue(HTMLProperty, value);
        }

        // Using a DependencyProperty as the backing store for HTML.  This enables animation, styling, binding, etc... 
        public static readonly DependencyProperty HTMLProperty = DependencyProperty.RegisterAttached("HTML", typeof(PocketItemWithContent), typeof(WebViewHtmlExtension), new PropertyMetadata("", new PropertyChangedCallback(OnHTMLChanged)));

        private static void OnHTMLChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebView wv = d as WebView;
            var item = (PocketItemWithContent)e.NewValue;
            if (wv != null && item != null)
            {


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
    }
    a { color: inherit; }
    img {
        max-width: 100% !important;
    }
</style>
</head>
<body>
<h1>"+item.Title+@"</h1>
" + item.Content + @"

</body>
</html>
";
                wv.NavigateToString(content);
            }
        }

    }
}
