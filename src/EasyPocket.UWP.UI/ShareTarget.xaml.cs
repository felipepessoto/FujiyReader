using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace EasyPocket.UWP.UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShareTarget : Page
    {
        ShareOperation shareOperation;

        public ShareTarget()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            shareOperation = (ShareOperation)e.Parameter;

            try
            {
                if (shareOperation.Data.Contains(StandardDataFormats.Uri))
                {
                    //await Task.Factory.StartNew(async () =>
                    //{
                    //shareOperation.ReportStarted();

                    var url = await shareOperation.Data.GetUriAsync();

                    TxbTitle.Text = "Adding " + url.ToString();

                    //shareOperation.ReportDataRetrieved();

                    await App.PocketClient.Add(url);

                    shareOperation.ReportCompleted();
                    //});
                }
            }
            catch (Exception ex)
            {
                shareOperation.ReportError(ex.ToString());
            }
        }
    }
}
