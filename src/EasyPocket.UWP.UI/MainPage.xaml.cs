using EasyPocket.Core;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace EasyPocket.UWP.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //EasyPocketClient pocketClient;

        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.PocketClient == null)
            {
                TblAuthenticating.Visibility = Visibility.Visible;
                //  pocketClient = await EasyPocketClient.Create();
                TblAuthenticating.Visibility = Visibility.Collapsed;
            }

            if (ViewModel == null)
            {
                ViewModel = new MainPageViewModel();

                var items = await App.PocketClient.Get();

                foreach (var item in items)
                {
                    var itemWithContent = await PocketItemWithContent.FromPocketItem(item);
                    ViewModel.Articles.Add(itemWithContent);
                }

                MasterListView.ItemsSource = ViewModel.Articles;
            }

            if (e.Parameter != null)
            {
                // Parameter is item ID
                var id = (string)e.Parameter;
                ViewModel.LastSelectedItem = ViewModel.Articles.Where((item) => item.ID == id).FirstOrDefault();
            }

            UpdateForVisualState(AdaptiveStates.CurrentState);

            // Don't play a content transition for first item load.
            // Sometimes, this content will be animated as part of the page transition.
            DisableContentTransitions();
        }

        private void AdaptiveStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            UpdateForVisualState(e.NewState, e.OldState);
        }

        private void UpdateForVisualState(VisualState newState, VisualState oldState = null)
        {
            var isNarrow = newState == NarrowState;

            if (isNarrow && oldState == DefaultState && ViewModel.LastSelectedItem != null)
            {
                // Resize down to the detail item. Don't play a transition.
                Frame.Navigate(typeof(DetailPage), ViewModel.LastSelectedItem.ID, new SuppressNavigationTransitionInfo());
            }

            EntranceNavigationTransitionInfo.SetIsTargetElement(MasterListView, isNarrow);
            if (DetailContentPresenter != null)
            {
                EntranceNavigationTransitionInfo.SetIsTargetElement(DetailContentPresenter, !isNarrow);
            }
        }

        private void MasterListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = (PocketItemWithContent)e.ClickedItem;
            ViewModel.LastSelectedItem = clickedItem;

            if (AdaptiveStates.CurrentState == NarrowState)
            {
                // Use "drill in" transition for navigating from master list to detail view
                Frame.Navigate(typeof(DetailPage), clickedItem.ID, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Assure we are displaying the correct item. This is necessary in certain adaptive cases.
            MasterListView.SelectedItem = ViewModel.LastSelectedItem;
        }

        private void EnableContentTransitions()
        {
            DetailContentPresenter.ContentTransitions.Clear();
            DetailContentPresenter.ContentTransitions.Add(new EntranceThemeTransition());
        }

        private void DisableContentTransitions()
        {
            if (DetailContentPresenter != null)
            {
                DetailContentPresenter.ContentTransitions.Clear();
            }
        }
    }

    public class MainPageViewModel
    {
        public MainPageViewModel()
        {
            Articles = new ObservableCollection<PocketItemWithContent>();
        }

        public ObservableCollection<PocketItemWithContent> Articles { get; set; }

        public PocketItemWithContent LastSelectedItem { get; set; }
    }
}
