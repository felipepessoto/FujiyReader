using FujiyReader.Core;
using PocketSharp.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace FujiyReader.UWP.UI
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

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
            Articles = new ObservableCollection<PocketItem>(localItems);
        }

        public ObservableCollection<PocketItem> Articles { get; set; }

        private PocketItem lastSelectedItem;

        public PocketItem LastSelectedItem
        {
            get { return lastSelectedItem; }
            set
            {
                lastSelectedItem = value;
                RaisePropertyChanged();
            }
        }

        public PocketItem ContextMenuItem { get; set; }

        //TODO mover pra OfflineContent class
        public DateTimeOffset LastSync
        {
            get
            {
                return (DateTimeOffset)(localSettings.Values["last_sync"] ?? DateTimeOffset.MinValue);
            }
            set
            {
                localSettings.Values["last_sync"] = value;
            }
        }

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

                Articles.Clear();

                foreach (var item in items)
                {
                    Articles.Add(item);
                }

                await SaveToLocalStorage();

                LastSync = DateTimeOffset.Now;
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
