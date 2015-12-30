using Newtonsoft.Json;
using PocketSharp;
using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace EasyPocket.Core
{
    public class EasyPocketClient
    {
        PocketClient client;
        private const string consumerKey = "49510-2b106efad3cb48ae12eab7f9";
        //Desktop 49510-2b106efad3cb48ae12eab7f9
        //Mobile 49510-89dd4b9e7a7314b66f780da3

        private string AccessToken
        {
            get
            {
                return (string)localSettings.Values["access_token"];
            }
            set
            {
                localSettings.Values["access_token"] = value;
            }
        }

        private string UserName
        {
            get
            {
                return (string)localSettings.Values["username"];
            }
            set
            {
                localSettings.Values["username"] = value;
            }
        }

        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localCacheFolder = Windows.Storage.ApplicationData.Current.LocalCacheFolder;

        private EasyPocketClient() { }

        public static async Task<EasyPocketClient> Create()
        {
            var easyPocketClient = new EasyPocketClient();

            await easyPocketClient.Initialize();

            return easyPocketClient;
        }

        private async Task Initialize()
        {
            if (AccessToken == null)
            {
                await Auth();
            }

            client = new PocketClient(consumerKey, AccessToken);
        }

        public async Task Auth()
        {
            var callback = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            string accessCode;

            using (HttpClient httpClient = new HttpClient())
            {

                var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("consumer_key", consumerKey),
                new KeyValuePair<string, string>("redirect_uri", callback)
            });

                var postResponse = await httpClient.PostAsync("https://getpocket.com/v3/oauth/request", content);
                var contentResponse = await postResponse.Content.ReadAsStringAsync();

                accessCode = ParseQueryString(contentResponse)["code"];
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

                    AccessToken = resultValues["access_token"];
                    UserName = resultValues["username"];
                }
            }
            else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                //TODO Console.WriteLine("HTTP Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseErrorDetail.ToString());
            }
            else
            {
                //TODO OutputToken("Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseStatus.ToString());
            }
        }

        public IEnumerable<PocketItemWithContent> GetLocalStorageItems()
        {
            if (localCacheFolder.TryGetItemAsync("Local_PocketItemWithContent").GetResults() != null)
            {
                using (var stream = new JsonTextReader(new StreamReader(localCacheFolder.OpenStreamForReadAsync("Local_PocketItemWithContent").Result)))
                {
                    IEnumerable<PocketItemWithContent> content = new JsonSerializer().Deserialize<IEnumerable<PocketItemWithContent>>(stream);
                    return content;
                }
            }

            return Enumerable.Empty<PocketItemWithContent>();
        }

        public async Task SetLocalStorageItems(IEnumerable<PocketItemWithContent> value)
        {
            using (JsonTextWriter jsonwriter = new JsonTextWriter(new StreamWriter(await localCacheFolder.OpenStreamForWriteAsync("Local_PocketItemWithContent", CreationCollisionOption.ReplaceExisting))))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonwriter, value);
            }

            //var jsonValue = JsonConvert.SerializeObject(value);
            //var localPocketCacheFile = await localCacheFolder.CreateFileAsync("Local_PocketItemWithContent", CreationCollisionOption.ReplaceExisting);
            //await FileIO.WriteTextAsync(localPocketCacheFile, jsonValue);
        }

        public Task<IEnumerable<PocketItem>> Get(RetrieveFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return client.Get(filter, cancellationToken);
        }

        public Task<PocketItem> Get(string itemID, CancellationToken cancellationToken = default(CancellationToken))
        {
            return client.Get(itemID, cancellationToken);
        }

        public Task<IEnumerable<PocketItem>> Get(State? state = default(State?), bool? favorite = default(bool?), string tag = null, ContentType? contentType = default(ContentType?), Sort? sort = default(Sort?), string search = null, string domain = null, DateTime? since = default(DateTime?), int? count = default(int?), int? offset = default(int?), CancellationToken cancellationToken = default(CancellationToken))
        {
            return client.Get(state, favorite, tag, contentType, sort, search, domain, since, count, offset, cancellationToken);
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(query);
            return decoder.ToDictionary(x => x.Name, x => x.Value);
        }
    }
}
