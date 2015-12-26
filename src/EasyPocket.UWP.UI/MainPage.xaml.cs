using PocketSharp;
using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EasyPocket.UWP.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string consumerKey = "49510-2b106efad3cb48ae12eab7f9";
        //Desktop 49510-2b106efad3cb48ae12eab7f9
        //Mobile 49510-89dd4b9e7a7314b66f780da3

        private static string accessToken;
        private static string accessCode;

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public MainPageViewModel ViewModel { get; set; }


        public MainPage()
        {
            this.InitializeComponent();
            ViewModel = new MainPageViewModel();
            accessToken = (string)localSettings.Values["access_token"];
        }

        public async Task Auth()
        {
            var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();

            using (HttpClient httpClient = new HttpClient())
            {

                var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("consumer_key", consumerKey),
                new KeyValuePair<string, string>("redirect_uri", callback)
            });

                var postResponse = await httpClient.PostAsync("https://getpocket.com/v3/oauth/request", content);
                var contentResponse = await postResponse.Content.ReadAsStringAsync();

                accessCode = contentResponse.Substring(5);
            }

            Uri StartUri = new Uri($"https://getpocket.com/auth/authorize?request_token={accessCode}&redirect_uri={callback}&webauthenticationbroker=1");

            WebAuthenticationResult WebAuthenticationResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, StartUri);
            if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                string token = WebAuthenticationResult.ResponseData.ToString();

                using (HttpClient httpClient = new HttpClient())
                {

                    var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("consumer_key", consumerKey),
                new KeyValuePair<string, string>("code", accessCode)
            });

                    var postResponse = await httpClient.PostAsync("https://getpocket.com/v3/oauth/authorize", content);
                    var contentResponse = await postResponse.Content.ReadAsStringAsync();

                    var resultValues = ParseQueryString(contentResponse);

                    localSettings.Values["access_token"] = accessToken = resultValues["access_token"];
                    var username = resultValues["username"];
                }
            }
            else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                //Console.WriteLine("HTTP Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseErrorDetail.ToString());
            }
            else
            {
                // OutputToken("Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseStatus.ToString());
            }
        }

        public static Dictionary<string, string> ParseQueryString(string query)
        {
            WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(query);
            return decoder.ToDictionary(x => x.Name, x => x.Value);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (accessToken == null)
            {
                TblAuthenticating.Visibility = Visibility.Visible;
                await Auth();
                TblAuthenticating.Visibility = Visibility.Collapsed;
            }

            PocketClient client = new PocketClient(consumerKey, accessToken);

            var items = await client.Get();

            foreach (var item in items)
            {
                ViewModel.Articles.Add(item);
            }
        }
    }

    public class MainPageViewModel
    {
        public MainPageViewModel()
        {
            Articles = new ObservableCollection<PocketItem>();
        }

        public ObservableCollection<PocketItem> Articles { get; set; }
    }
}
