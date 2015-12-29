using PocketSharp.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace EasyPocket.Core
{
    public class PocketItemWithContent : PocketItem
    {
        public string Content { get; set; }

        public static async Task<PocketItemWithContent> FromPocketItem(PocketItem item)
        {
            string content;
            using (HttpClient httpClient = new HttpClient())
            {
                content = await httpClient.GetStringAsync(item.Uri);
            }

            content = Html2Article.GetArticle(content).ContentWithTags;

            return new PocketItemWithContent
            {
                ID = item.ID,
                Title = item.Title,
                Excerpt = item.Excerpt,
                Uri = item.Uri,
                Content = content,
            };
        }
    }
}
