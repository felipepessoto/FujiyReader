using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace FujiyReader.Core
{
    public class ArticleContentVerticalPosition
    {
        static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        static ApplicationDataContainer container = localSettings.CreateContainer("ArticleContentVerticalPosition", ApplicationDataCreateDisposition.Always);

        public static int GetVerticalPosition(PocketItem item)
        {
            int position = (int) (container.Values[item.ID] ?? 0);

            return position;
        }

        public static void SetVerticalPosition(PocketItem item, int position)
        {
            container.Values[item.ID] = position;
        }
    }
}
