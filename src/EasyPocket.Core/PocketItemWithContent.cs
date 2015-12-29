using PocketSharp.Models;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
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


        public static async Task<PocketItemWithContent> FromPocketItem(PocketItem item)
        {
            string content = "Error to load the article :(";

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    content = await httpClient.GetStringAsync(item.Uri);
                }
                content = Html2Article.GetArticle(content).ContentWithTags;
            }
            catch (Exception)
            {
                content = "Error to load the article :(";
            }



            return new PocketItemWithContent
            {
                ID = item.ID,
                Title = item.Title,
                Excerpt = item.Excerpt,
                Uri = item.Uri,
                Content = content,
            };
        }


        public override void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            base.OnPropertyChanged(caller);
        }
    }
}
