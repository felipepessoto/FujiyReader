using EasyPocket.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyPocket.UWP.UI
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        public MainPageViewModel()
        {
            Articles = new ObservableCollection<PocketItemWithContent>();
        }

        public ObservableCollection<PocketItemWithContent> Articles { get; set; }


        private PocketItemWithContent lastSelectedItem;

        public PocketItemWithContent LastSelectedItem
        {
            get { return lastSelectedItem; }
            set
            {
                lastSelectedItem = value;
                RaisePropertyChanged();
            }
        }

        public async Task Sync()
        {
            var items = await App.PocketClient.Get();

            Articles.Clear();

            foreach (var item in items)
            {
                var itemWithContent = await PocketItemWithContent.FromPocketItem(item);
                Articles.Add(itemWithContent);
            }
        }
    }
}
