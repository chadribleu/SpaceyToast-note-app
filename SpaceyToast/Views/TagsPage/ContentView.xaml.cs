using SpaceyToast.Core.Services;
using SpaceyToast.Source.User;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SpaceyToast.Views.TagsPage
{
    public sealed partial class ContentView : Page
    {
        private ObservableCollection<TagManifestData> ManifestDataList { get; }

        public ContentView()
        {
            InitializeComponent();
            ManifestDataList = new ObservableCollection<TagManifestData>();
            NotesGridView.ItemsSource = ManifestDataList;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter as string == null)
            {
                return;
            }

            TagNameText.Text = e.Parameter as string;

            List<TagManifestData> dataList = (await TagManifestManager.Instance.Get())
                .Where(d => d.Tags.Contains(e.Parameter as string))
                .ToList();

            dataList.ForEach(ManifestDataList.Add);
        }
        
        private void NotesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Frame.Navigate(typeof(DrawingArea), e.ClickedItem as TagManifestData);
        }
    }
}
