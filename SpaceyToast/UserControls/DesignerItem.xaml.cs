using System;
using System.ComponentModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace SpaceyToast.UserControls
{
    public sealed partial class DesignerItem : UserControl, INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public static readonly DependencyProperty ItemContentProperty =
            DependencyProperty.Register("ItemContent", typeof(object), typeof(DesignerItem), null);
        public object ItemContent
        {
            get { return GetValue(ItemContentProperty); }
            set
            {
                SetValue(ItemContentProperty, value);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (IsSelected)
            {
                this.Focus(FocusState.Programmatic);
            }
        }

        public DesignerItem(object content)
        {
            this.InitializeComponent();
            
            ItemContent = content;
            ContentItem.DataContext = this;
        }

        public Rect GetLocation()
        {
            FrameworkElement content = ItemContent as FrameworkElement;
            return new Rect(Canvas.GetLeft(ContentItem), Canvas.GetTop(ContentItem), 
                content.ActualWidth, content.ActualHeight);
        }

        public void UpdateSize(double width, double height)
        {
            ContentItem.Width = width;
            ContentItem.Height = height;
        }

        public void ManualThumbsAdjust(double left, double top)
        {
            Canvas.SetLeft(ContentItem, left);
            Canvas.SetTop(ContentItem, top);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Control item = (sender as Control).DataContext as Control;
            
            if (item != null)
            {
                double left = Canvas.GetLeft(item);
                double top = Canvas.GetTop(item);

                Canvas.SetLeft(item, left + e.HorizontalChange);
                Canvas.SetTop(item, top + e.VerticalChange);
            }
        }

        private void Thumb_DragDelta_1(object sender, DragDeltaEventArgs e)
        {
            Control target = (sender as Control);
            Control item = (target.DataContext as Control);

            if (item != null)
            {
                double deltaVertical = 0;
                double deltaHorizontal = 0;

                // fix: resizing Adorners
                item.Height = item.ActualHeight;
                item.Width = item.ActualWidth;

                // get alignement
                switch (target.VerticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                        deltaVertical = Math.Min(-e.VerticalChange, item.ActualHeight - item.MinHeight);
                        break;
                    case VerticalAlignment.Top:
                        deltaVertical = Math.Min(e.VerticalChange, item.ActualHeight - item.MinHeight);
                        Canvas.SetTop(item, Canvas.GetTop(item) + deltaVertical);
                        break;
                    default:
                        break;
                }
                switch (target.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        deltaHorizontal = Math.Min(e.HorizontalChange, item.ActualWidth - item.MinWidth);
                        Canvas.SetLeft(item, Canvas.GetLeft(item) + deltaHorizontal);
                        break;
                    case HorizontalAlignment.Right:
                        deltaHorizontal = Math.Min(-e.HorizontalChange, item.ActualWidth - item.MinWidth);
                        break;
                    default:
                        break;
                }
                if ((item.Height - deltaVertical) > item.MinHeight) item.Height -= deltaVertical;
                if ((item.Width - deltaHorizontal) > item.MinWidth) item.Width -= deltaHorizontal;
            }
        }

        private void ContentItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (ContentItem.Focus(FocusState.Pointer))
            {
                this.IsSelected = true;
            }
        }

        private void ContentItem_LostFocus(object sender, RoutedEventArgs e)
        {
            this.IsSelected = false;
        }

        private void ContentItem_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Escape)
            {
                this.IsSelected = false;
            }
        }
    }
}