using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FujiyReader.Core
{
    internal class OfflineContent
    {
        static StorageFolder localCacheFolder = ApplicationData.Current.LocalFolder;

        public static async Task<PocketArticle> GetArticle(PocketItem item)
        {
            StorageFolder articlesFolder = await localCacheFolder.CreateFolderAsync("PocketArticles", CreationCollisionOption.OpenIfExists);
            string filename = item.ID;
            bool fileExists = (await articlesFolder.TryGetItemAsync(filename)) != null;
            
            if (fileExists)
            {
                PocketArticle cachedContent = await JsonStorage.ExtractFromJsonFile<PocketArticle>(articlesFolder, filename);
                if (cachedContent != null)
                {
                    return cachedContent;
                }
            }

            return null;
        }

        public static async Task SaveArticle(PocketArticle pa)
        {
            StorageFolder articlesFolder = await localCacheFolder.CreateFolderAsync("PocketArticles", CreationCollisionOption.OpenIfExists);
            await JsonStorage.SaveToJsonFile(articlesFolder, pa.ID, pa);
        }
    }
}
