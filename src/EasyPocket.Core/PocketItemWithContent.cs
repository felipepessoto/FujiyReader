using PocketSharp.Models;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPocket.Core
{
    public class PocketItemWithContent : PocketItem
    {
        private string content;

        public string Content
        {
            get { return content; }
            set
            {
                content = value;
                OnPropertyChanged();
            }
        }

        private int scrollVerticalPosition;

        public int ScrollVerticalPosition
        {
            get { return scrollVerticalPosition; }
            set
            {
                scrollVerticalPosition = value;
                OnPropertyChanged();
            }
        }

        private static async Task LoadContent(EasyPocketClient client, PocketItemWithContent item, bool forceRefresh)
        {
            try
            {
                item.Content = (await client.GetArticle(item.Uri, forceRefresh: forceRefresh)).Content;
            }
            catch (Exception)
            {
                //content = "Error to load the article :(";
            }
        }


        public static async Task<PocketItemWithContent> FromPocketItem(EasyPocketClient client, PocketItem item, bool forceRefresh)
        {
            var itemWithContent = new PocketItemWithContent
            {
                ID = item.ID,
                Title = item.Title,
                Excerpt = item.Excerpt,
                Uri = item.Uri,
                ScrollVerticalPosition = ((await client.GetLocalStorageItem(item.ID))?.ScrollVerticalPosition).GetValueOrDefault(),
            };

            LoadContent(client, itemWithContent, forceRefresh);

            return itemWithContent;
        }


        public override void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            base.OnPropertyChanged(caller);
        }
    }
}
