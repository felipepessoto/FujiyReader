﻿using EasyPocket.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
            //TODO Necessario?
            //foreach (var item in Articles)
            //{
            //    item.PropertyChanged -= Item_PropertyChanged;
            //}

            Articles.Clear();

            var itemsWithContent = (await App.PocketClient.Get()).Select(PocketItemWithContent.FromPocketItem).ToList();

            foreach (var item in itemsWithContent)
            {
                item.PropertyChanged += Item_PropertyChanged;
                Articles.Add(item);
            }

            await SaveToLocalStorage();
        }

        static SemaphoreSlim semaphoreQueue = new SemaphoreSlim(2, 2);
        static SemaphoreSlim semaphoreLock = new SemaphoreSlim(1, 1);

        private async void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO fazer throttle
            if (e.PropertyName == "Content")
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
                                await Task.Delay(3000);
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
            await App.PocketClient.SetLocalStorageItems(Articles);
        }
    }
}
