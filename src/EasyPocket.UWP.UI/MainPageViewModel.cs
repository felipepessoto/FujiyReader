using EasyPocket.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

        private MainPageViewModel()
        {
            
        }

        public static async Task<MainPageViewModel> Create()
        {
            var mainPageViewModel = new MainPageViewModel();

            await mainPageViewModel.Initialize();

            return mainPageViewModel;
        }

        private async Task Initialize()
        {
            var localItems = await App.PocketClient.GetLocalStorageItems();
            Articles = new ObservableCollection<PocketItemWithContent>(localItems);
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
            var items = (await App.PocketClient.Get()).ToArray();

            PocketItemWithContent[] itemsWithContent = new PocketItemWithContent[items.Length];
            Task<PocketItemWithContent>[] tasks = new Task<PocketItemWithContent>[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                tasks[i] = PocketItemWithContent.FromPocketItem(items[i]);
            }

            await Task.WhenAll(tasks);

            Articles.Clear();

            for (int i = 0; i < tasks.Length; i++)
            {
                itemsWithContent[i] = tasks[i].Result;
                Articles.Add(tasks[i].Result);
            }

            await App.PocketClient.SetLocalStorageItems(itemsWithContent);
        }
    }
}
