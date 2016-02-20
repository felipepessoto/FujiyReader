using PocketSharp.Models;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FujiyReader.Core
{
    //TODO apagar esta classe
    public class PocketItemWithContent : PocketItem
    {
        public static async Task<PocketItemWithContent> FromPocketItem(FujiyReaderClient client, PocketItem item, bool forceRefresh)
        {
            //TODO verificar props read only
            var itemWithContent = new PocketItemWithContent
            {
                ID = item.ID,
                AddTime = item.AddTime,
                Authors = item.Authors,
                Excerpt = item.Excerpt,
                FavoriteTime = item.FavoriteTime,
                FullTitle = item.FullTitle,
                //HasImage = item.HasImage,
                //HasVideo = item.HasVideo,
                Images = item.Images,
                //IsArchive = item.IsArchive,
                IsArticle = item.IsArticle,
                //IsDeleted = item.IsDeleted,
                IsFavorite = item.IsFavorite,
                //IsImage = item.IsImage,
                //IsVideo = item.IsVideo,
                Json = item.Json,
                //LeadImage = item.LeadImage,
                ReadTime = item.ReadTime,
                ResolvedId = item.ResolvedId,
                Sort = item.Sort,
                Status = item.Status,
                Tags = item.Tags,
                //TagsString = item.TagsString,
                Title = item.Title,
                UpdateTime = item.UpdateTime,
                Uri = item.Uri,
                Videos = item.Videos,
                WordCount = item.WordCount,
            };

            return itemWithContent;
        }


        public override void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            base.OnPropertyChanged(caller);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            PocketItem item = obj as PocketItem;

            if (item == null)
            {
                return false;
            }

            return ID == item.ID;
        }


        

    }
}
