using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FujiyReader.Core
{
    internal class JsonStorage
    {
        public static async Task SaveToJsonFile(StorageFolder folder, string filename, object value)
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

        public static async Task<T> ExtractFromJsonFile<T>(StorageFolder folder, string filename) where T : class
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
    }
}
