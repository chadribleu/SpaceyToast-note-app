using SpaceyToast.Core.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace SpaceyToast.Views.TagsPage
{
    public sealed partial class Host : Page
    {
        private ObservableCollection<string> TagsCollection { get; }

        public Host()
        {
            InitializeComponent();
            TagsCollection = new ObservableCollection<string>();
            TagsList.ItemsSource = TagsCollection;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            List<string> tags = await TagManagerService.Instance.GetGlobalTags();
            for (int i = 0; i < tags.Count; i++)
            {
                TagsCollection.Add(tags[i]);
            }
        }

        private void TagsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(ContentView), e.ClickedItem as string, new SuppressNavigationTransitionInfo());
        }
    }
}
