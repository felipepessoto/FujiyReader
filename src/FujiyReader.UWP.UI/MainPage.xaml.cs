using FujiyReader.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FujiyReader.UWP.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPageViewModel ViewModel { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ViewModel == null)
            {
                ViewModel = await MainPageViewModel.Create();
                if (ViewModel.LastSync < DateTimeOffset.Now.AddHours(-1))
                {
                    await ViewModel.Sync();
                }
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
                Frame.Navigate(typeof(DetailPage), ViewModel, new SuppressNavigationTransitionInfo());
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
                Frame.Navigate(typeof(DetailPage), ViewModel, new DrillInNavigationTransitionInfo());
            }
            else
            {
                // Play a refresh animation when the user switches detail items.
                EnableContentTransitions();
            }
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

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            WebViewHtmlExtension.WebView_NavigationCompleted(sender, (PocketItemWithContent)sender.DataContext);
        }

        private void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            WebViewHtmlExtension.WebView_ScriptNotify((PocketItemWithContent)((WebView)sender).DataContext, e);
        }

        private bool _IsShiftPressed = false;
        private bool _IsPointerPressed = false;

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            // Handle Shift+F10
            // Handle MenuKey

            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                _IsShiftPressed = true;
            }

            // Shift+F10
            else if (_IsShiftPressed && e.Key == Windows.System.VirtualKey.F10)
            {
                var FocusedElement = FocusManager.GetFocusedElement() as UIElement;

                PocketItemWithContent MyObject = null;
                if (FocusedElement is ContentControl)
                {
                    MyObject = ((ContentControl)FocusedElement).Content as PocketItemWithContent;
                }
                ShowContextMenu(MyObject, FocusedElement, new Point(0, 0));
                e.Handled = true;
            }

            // The 'Menu' key next to Right Ctrl on most keyboards
            else if (e.Key == Windows.System.VirtualKey.Application)
            {
                var FocusedElement = FocusManager.GetFocusedElement() as UIElement;
                PocketItemWithContent MyObject = null;
                if (FocusedElement is ContentControl)
                {
                    MyObject = ((ContentControl)FocusedElement).Content as PocketItemWithContent;
                }
                ShowContextMenu(MyObject, FocusedElement, new Point(0, 0));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Shift)
            {
                _IsShiftPressed = false;
            }

            base.OnKeyUp(e);
        }
        protected override void OnHolding(HoldingRoutedEventArgs e)
        {
            // Responding to HoldingState.Started will show a context menu while your finger is still down, while 
            // HoldingState.Completed will wait until the user has removed their finger. 
            if (e.HoldingState == Windows.UI.Input.HoldingState.Started)
            {
                var PointerPosition = e.GetPosition(null);

                var MyObject = (e.OriginalSource as FrameworkElement).DataContext as PocketItemWithContent;
                ShowContextMenu(MyObject, null, PointerPosition);
                e.Handled = true;

                // This, combined with a check in OnRightTapped prevents the firing of RightTapped from
                // launching another context menu
                _IsPointerPressed = false;

                // This prevents any scrollviewers from continuing to pan once the context menu is displayed.  
                // Ideally, you should find the ListViewItem itself and only CancelDirectMinpulations on that item.  
                var ItemsToCancel = VisualTreeHelper.FindElementsInHostCoordinates(PointerPosition, MasterListView);
                foreach (var Item in ItemsToCancel)
                {
                    var Result = Item.CancelDirectManipulations();
                }
            }

            base.OnHolding(e);
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            _IsPointerPressed = true;

            base.OnPointerPressed(e);
        }

        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            if (_IsPointerPressed)
            {
                var MyObject = (e.OriginalSource as FrameworkElement).DataContext as PocketItemWithContent;

                ShowContextMenu(MyObject, null, e.GetPosition(null));
                e.Handled = true;
            }

            base.OnRightTapped(e);
        }

        private void ShowContextMenu(PocketItemWithContent data, UIElement target, Point offset)
        {
            ViewModel.ContextMenuItem = data;

            var MyFlyout = this.Resources["SampleContextMenu"] as MenuFlyout;

            System.Diagnostics.Debug.WriteLine("MenuFlyout shown '{0}', '{1}'", target, offset);

            MyFlyout.ShowAt(target, offset);
        }
    }
}
