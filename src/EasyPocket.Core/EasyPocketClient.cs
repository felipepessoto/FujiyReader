using Newtonsoft.Json;
using PocketSharp;
using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        Windows.Storage.StorageFolder localCacheFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

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

            client = new PocketClient(consumerKey, AccessToken, parserUri: new Uri("http://text.getpocket.com/v3/text"));
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

        const string Local_PocketItemWithContent = "Local_PocketItemWithContent";

        public async Task<IEnumerable<PocketItemWithContent>> GetLocalStorageItems()
        {
            IEnumerable<PocketItemWithContent> content = await ExtractFromJsonFile<IEnumerable<PocketItemWithContent>>(localCacheFolder, Local_PocketItemWithContent);

            return content ?? Enumerable.Empty<PocketItemWithContent>();
        }

        public async Task<PocketItemWithContent> GetLocalStorageItem(string id)
        {
            PocketItemWithContent content = (await ExtractFromJsonFile<IEnumerable<PocketItemWithContent>>(localCacheFolder, Local_PocketItemWithContent))?.SingleOrDefault(x => x.ID == id);

            return content;
        }

        public Task SetLocalStorageItems(IEnumerable<PocketItemWithContent> value)
        {
            return SaveToJsonFile(localCacheFolder, Local_PocketItemWithContent, value);
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

        public async Task<PocketArticle> GetArticle(Uri uri, bool includeImages = true, bool includeVideos = true, bool forceRefresh = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            StorageFolder articlesFolder = await localCacheFolder.CreateFolderAsync("PocketArticles", CreationCollisionOption.OpenIfExists);
            string filename = GenerateFileNameFromUri(uri);
            bool fileExists = false;

            if (forceRefresh == false)
            {
                fileExists = (await articlesFolder.TryGetItemAsync(filename)) != null;
            }

            if (fileExists == false)
            {
                PocketArticle content = await client.GetArticle(uri, includeImages, includeVideos, forceRefresh, cancellationToken);
                await SaveToJsonFile(articlesFolder, filename, content);
                return content;
            }
            else
            {
                PocketArticle content = await ExtractFromJsonFile<PocketArticle>(articlesFolder, filename);
                return content;
            }
        }

        public Task<PocketItem> Add(Uri uri)
        {
            return client.Add(uri);
        }

        public Task<bool> MarkRead(string itemId)
        {
            return client.Archive(itemId);
        }

        public Task<bool> Delete(string itemId)
        {
            return client.Delete(itemId);
        }

        private string GenerateFileNameFromUri(Uri uri)
        {
            return WebUtility.UrlEncode(uri.ToString());
        }

        private async Task SaveToJsonFile(StorageFolder folder, string filename, object value)
        {
            //using (var stream = await localCacheFolder.OpenStreamForWriteAsync(Local_PocketItemWithContent, CreationCollisionOption.ReplaceExisting))
            //using (var streamWriter = new StreamWriter(stream))
            //using (JsonTextWriter jsonwriter = new JsonTextWriter(streamWriter))
            //{
            //    var serializer = new JsonSerializer();
            //    serializer.Serialize(jsonwriter, value);
            //}

            var localPocketCacheFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(localPocketCacheFile, JsonConvert.SerializeObject(value));
        }

        private async Task<T> ExtractFromJsonFile<T>(StorageFolder folder, string filename) where T : class
        {
            T content = null;
            if (await folder.TryGetItemAsync(filename) != null)
            {
                using (var stream = new JsonTextReader(new StreamReader(await folder.OpenStreamForReadAsync(filename))))
                {
                    content = new JsonSerializer().Deserialize<T>(stream);
                }
            }

            return content;
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            WwwFormUrlDecoder decoder = new WwwFormUrlDecoder(query);
            return decoder.ToDictionary(x => x.Name, x => x.Value);
        }
    }
}
