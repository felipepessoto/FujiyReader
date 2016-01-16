using FujiyReader.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FujiyReader.UWP.UI
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

        private MainPageViewModel() { }

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

            AttachAutoSave(Articles);
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

        public PocketItemWithContent ContextMenuItem { get; set; }

        private bool canSync = true;
        public bool CanSync
        {
            get { return canSync; }
            set
            {
                canSync = value;
                RaisePropertyChanged();
            }
        }

        public async Task Sync()
        {
            CanSync = false;

            try
            {
                //TODO Necessario?
                //foreach (var item in Articles)
                //{
                //    item.PropertyChanged -= Item_PropertyChanged;
                //}

                var items = await App.PocketClient.Get();

                List<PocketItemWithContent> itemsWithContent = new List<PocketItemWithContent>();

                foreach (var item in items)
                {
                    var itemWithContent = await PocketItemWithContent.FromPocketItem(App.PocketClient, item, false);
                    itemsWithContent.Add(itemWithContent);
                }

                Articles.Clear();

                foreach (var item in itemsWithContent)
                {
                    Articles.Add(item);
                }

                await SaveToLocalStorage();

                AttachAutoSave(Articles);
            }
            finally
            {
                CanSync = true;
            }
        }

        public Task MarkRead()
        {
            var item = ContextMenuItem;

            return App.PocketClient.MarkRead(item.ID);
        }

        public Task Delete()
        {
            var item = ContextMenuItem;

            return App.PocketClient.Delete(item.ID);
        }

        private void AttachAutoSave(IEnumerable<PocketItemWithContent> items)
        {
            foreach (var item in items)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        static SemaphoreSlim semaphoreQueue = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaphoreLock = new SemaphoreSlim(1, 1);

        private async void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO fazer throttle
            if (e.PropertyName == "Content" || e.PropertyName == "ScrollVerticalPosition")
            {
                if (await semaphoreQueue.WaitAsync(0))
                {
                    try
                    {
                        if (await semaphoreLock.WaitAsync(TimeSpan.FromSeconds(10)))
                        {
                            try
                            {
                                await SaveToLocalStorage();
                            }
                            finally
                            {
                                semaphoreLock.Release();
                            }
                        }
                    }
                    finally
                    {
                        semaphoreQueue.Release();
                    }
                }
            }
        }

        public async Task SaveToLocalStorage()
        {
            //TODO mudar storage para um arquivo por artigo

            await App.PocketClient.SetLocalStorageItems(Articles);
        }
    }
}
