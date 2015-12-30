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

        private static async Task LoadContent(PocketItemWithContent item)
        {
            string content;
            try
            {
                using (HttpClient httpClient = new HttpClient(new RetryDelegatingHandler()))
                {
                    content = await httpClient.GetStringAsync(item.Uri);
                }
                item.Content = Html2Article.GetArticle(content).ContentWithTags;
            }
            catch (Exception)
            {
                //content = "Error to load the article :(";
            }
        }


        public static PocketItemWithContent FromPocketItem(PocketItem item)
        {
            var itemWithContent = new PocketItemWithContent
            {
                ID = item.ID,
                Title = item.Title,
                Excerpt = item.Excerpt,
                Uri = item.Uri,
            };

            LoadContent(itemWithContent);

            return itemWithContent;
        }


        public override void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            base.OnPropertyChanged(caller);
        }
    }

    public class RetryDelegatingHandler : DelegatingHandler
    {
        public int MaxRetries { get; set; } = 3;


        public RetryDelegatingHandler() : this(new HttpClientHandler())
        { }

        public RetryDelegatingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;

            int i = 0;
            do
            {
                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    return response;
                }
                catch (Exception ex) when (i<= MaxRetries)
                {

                }

                i++;
            } while (true);
        }
    }
}
