using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using SpaceyToast.Core.Services;
using SpaceyToast.Models;
using SpaceyToast.Source.User;
using SpaceyToast.UserControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

using CurrentDrawingSession = SpaceyToast.Source.SaveDataManager<SpaceyToast.Source.User.BoardData>;
using CurrentUserSettings = SpaceyToast.Source.SaveDataManager<SpaceyToast.Source.User.CurrentUserData>;

namespace SpaceyToast.Views
{
    public sealed partial class DrawingArea : Page
    {
        private const int c_MaxUndo = 64;

        private enum ObjectType { Bitmap }
        private enum UndoRedoRequest { Ink, Bitmap }
        private CurrentTool _ActiveTool;

        private List<UndoRedoRequest> _UndoRequests = new List<UndoRedoRequest>(c_MaxUndo);
        private bool _isAreaMoving = false;

        // Selection
        private Polyline _LassoSel;
        private bool _Selecting = false;
        private bool _HasActiveSelection = false;
        private int s_LayerIndex = 0;
        private Rect _SelectionBoundingRect;
        private CoreWetStrokeUpdateSource _CoreWetStrokeUpdateSource;

        // Pens
        InkDrawingAttributes _GlobalDrawingAttributes = new InkDrawingAttributes();

        // Manifest
        private TagManifestData ManifestData { get; set; }
        private ObservableCollection<TagsLinker> _TagsLinker;

        // Viewport
        private Point _lastPoint;

        public DrawingArea()
        {
            var saveData = CurrentUserSettings.Instance.SaveData;
            double tempPenSize = saveData.Atelier.PencilSize;
            double tempPenOpacity = saveData.Atelier.PencilOpacity;
            double tempBrushSize = saveData.Atelier.BrushSize;

            InitializeComponent();

            InkQuantitySlider.Value = tempPenSize;
            OpacitySlider.Value = tempPenOpacity;
            BrushSizeSlider.Value = tempBrushSize;

            // enable devices for input
            InkRenderer.InkPresenter.InputDeviceTypes =
                  Windows.UI.Core.CoreInputDeviceTypes.Mouse;

            // listen to input events
            InkRenderer.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;

            _CoreWetStrokeUpdateSource = CoreWetStrokeUpdateSource.Create(InkRenderer.InkPresenter);
            _CoreWetStrokeUpdateSource.WetStrokeStarting += _CoreWetStrokeUpdateSource_WetStrokeStarting;
            _CoreWetStrokeUpdateSource.WetStrokeContinuing += _CoreWetStrokeUpdateSource_WetStrokeContinuing;

            SelectionCommandBar.RegisterPropertyChangedCallback(CommandBar.VisibilityProperty, (sender, dp) =>
            {
                if ((Visibility)sender.GetValue(dp) == Visibility.Visible)
                {
                    bool isButtonEnabled = InkRenderer.InkPresenter.StrokeContainer.CanPasteFromClipboard();

                    CommandBarCopyButton.IsEnabled = _HasActiveSelection;
                    CommandBarCutButton.IsEnabled = _HasActiveSelection;

                    CommandBarPasteButton.IsEnabled = isButtonEnabled;
                    CommandBarDeleteButton.IsEnabled = true;
                }
            });

            _TagsLinker = new ObservableCollection<TagsLinker>();
            AddTagsHost.ItemsSource = _TagsLinker;
        }

        private void StrokeInput_StrokeEnded(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            _UndoRequests.Add(UndoRedoRequest.Ink);
            if (_HasActiveSelection)
                sender.InkPresenter.StrokeContainer.GetStrokes().Last().Selected = true;
        }

        private void HandleStrokes(CoreWetStrokeUpdateEventArgs args)
        {
            for (int i = 0; i < args.NewInkPoints.Count; ++i)
            {
                if (!_SelectionBoundingRect.Contains(args.NewInkPoints[i].Position))
                {
                    args.NewInkPoints.RemoveAt(i);
                    args.Disposition = CoreWetStrokeDisposition.Completed;
                }
            }
        }

        private void _CoreWetStrokeUpdateSource_WetStrokeStarting(CoreWetStrokeUpdateSource sender, CoreWetStrokeUpdateEventArgs args)
        {
            if (_HasActiveSelection)
            {
                HandleStrokes(args);
            }
            args.Disposition = CoreWetStrokeDisposition.Inking;
        }

        private void _CoreWetStrokeUpdateSource_WetStrokeContinuing(CoreWetStrokeUpdateSource sender, CoreWetStrokeUpdateEventArgs args)
        {
            if (_HasActiveSelection)
            {
                HandleStrokes(args);
            }
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            switch (_ActiveTool)
            {
                case CurrentTool.FreeSelection:
                    if (!_SelectionBoundingRect.Contains(args.CurrentPoint.RawPosition))
                    {
                        _Selecting = false;
                        _HasActiveSelection = false;
                        OverlayCanvas.Children.Clear();
                        SelectionCommandBar.Visibility = Visibility.Collapsed;
                    }
                    if (!_HasActiveSelection)
                    {
                        _LassoSel = new Polyline()
                        {
                            StrokeThickness = 2,
                            Stroke = new SolidColorBrush(Windows.UI.Colors.Magenta),
                            StrokeDashArray = new DoubleCollection() { 4, 6, 1, 4 },
                            Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 0, 255))
                        };
                        _LassoSel.Points.Add(args.CurrentPoint.RawPosition);
                        OverlayCanvas.Children.Add(_LassoSel);
                        _Selecting = true;
                        break;
                    }
                    break;
            }
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_Selecting)
            {
                _LassoSel.Points.Add(args.CurrentPoint.RawPosition);
            }
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            if (_Selecting)
            {
                _Selecting = false;

                if (OverlayCanvas.Children.Count > 0)
                {
                    OverlayCanvas.Children.Clear();
                }

                _SelectionBoundingRect = InkRenderer.InkPresenter.StrokeContainer.SelectWithPolyLine(_LassoSel.Points);

                if (_SelectionBoundingRect.X == 0 && _SelectionBoundingRect.Y == 0 &&
                    _SelectionBoundingRect.Width == 0 && _SelectionBoundingRect.Height == 0)
                    return;

                // Draw square selection around the bounding rect
                Rectangle bounds = new Rectangle()
                {
                    Stroke = new SolidColorBrush(Windows.UI.Colors.Magenta),
                    StrokeThickness = 2,
                    Width = _SelectionBoundingRect.Width,
                    Height = _SelectionBoundingRect.Height,
                };
                bounds.IsHitTestVisible = false;
                Canvas.SetLeft(bounds, _SelectionBoundingRect.X);
                Canvas.SetTop(bounds, _SelectionBoundingRect.Y);
                _HasActiveSelection = true;
                OverlayCanvas.Children.Add(bounds);

                // restore command bar states for copy-paste requests
                SelectionCommandBar.Visibility = Visibility.Visible;
            }
        }

        private async Task Layer_AddObject(ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Bitmap:
                    try
                    {
                        FileOpenPicker fp = new FileOpenPicker();
                        fp.FileTypeFilter.Add(".jpg");
                        fp.FileTypeFilter.Add(".bmp");
                        fp.FileTypeFilter.Add(".png");

                        StorageFile bitmapFile = await fp.PickSingleFileAsync();
                        if (bitmapFile != null)
                        {
                            IRandomAccessStream stream = await bitmapFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                            var target = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                            await target.SetSourceAsync(stream);

                            int pixelsWidth = target.PixelWidth;
                            int pixelsHeight = target.PixelHeight;

                            // added uniform scale
                            if (pixelsWidth > ObjectLayer.ActualWidth || pixelsHeight > ObjectLayer.ActualHeight)
                            {
                                double preferredWidth = DrawingView.ActualWidth;
                                double quotient = (double)pixelsHeight / (double)pixelsWidth;

                                pixelsWidth = (int)Math.Round(preferredWidth);
                                pixelsHeight = (int)Math.Round(quotient * preferredWidth);
                            }
                            var img = new Image();
                            img.Source = target;
                            img.Stretch = Stretch.Fill;

                            var image = new DesignerItem(img);
                            image.UpdateSize((double)pixelsWidth, (double)pixelsHeight);

                            ObjectLayer.Children.Add(image);

                            float centerX = (float)DrawingView.ActualWidth / 2;
                            float centerY = (float)DrawingView.ActualHeight / 2;

                            image.ManualThumbsAdjust(centerX - pixelsWidth / 2, centerY - pixelsHeight / 2);

                            Canvas.SetZIndex(image, s_LayerIndex);
                            image.IsSelected = true;

                            _UndoRequests.Add(UndoRedoRequest.Bitmap);
                            SpaceyToastToolBar.ActiveTool = CursorTool;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
            }
            ++s_LayerIndex;
        }

        private async void AddPicture_Click(object sender, RoutedEventArgs e)
        {
            _ActiveTool = CurrentTool.AddBitmap;
            await Layer_AddObject(ObjectType.Bitmap);
        }

        private void SelectionToolFree_Click(object sender, RoutedEventArgs e)
        {
            InkRenderer.InkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            InkRenderer.InkPresenter.UnprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            InkRenderer.InkPresenter.UnprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            InkRenderer.InkPresenter.UnprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;

            _ActiveTool = CurrentTool.FreeSelection;
        }

        private void InkToolbar_ActiveToolChanged(InkToolbar sender, object args)
        {
            // enable input for pen, brush and eraser
            InkRenderer.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;

            if (sender.ActiveTool.Equals(PencilTool) || sender.ActiveTool.Equals(BrushTool))
            {
                InkRenderer.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Inking;

                ColorsToolbar.Visibility = Visibility.Visible;

                if (sender.ActiveTool.Equals(PencilTool))
                {
                    InkDrawingAttributes pencilDrawingAttributes = InkDrawingAttributes.CreateForPencil();
                    pencilDrawingAttributes.Color = _GlobalDrawingAttributes.Color;
                    pencilDrawingAttributes.Size = new Size(InkQuantitySlider.Value, InkQuantitySlider.Value);
                    pencilDrawingAttributes.PencilProperties.Opacity = OpacitySlider.Value;
                    _GlobalDrawingAttributes = pencilDrawingAttributes;
                    InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);

                    CurrentUserSettings.Instance.SaveData.Atelier.LastTool = (_ActiveTool = CurrentTool.Pencil);
                }
                else
                {
                    InkDrawingAttributes brushDrawingAttributes = new InkDrawingAttributes();
                    brushDrawingAttributes.Color = _GlobalDrawingAttributes.Color;
                    brushDrawingAttributes.Size = new Size(BrushSizeSlider.Value, BrushSizeSlider.Value);
                    brushDrawingAttributes.PenTip = CurrentUserSettings.Instance.SaveData.Atelier.PencilShape;
                    _GlobalDrawingAttributes = brushDrawingAttributes;
                    InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);

                    CurrentUserSettings.Instance.SaveData.Atelier.LastTool = (_ActiveTool = CurrentTool.Brush);
                }
                return;
            }

            ColorsToolbar.Visibility = Visibility.Collapsed;

            if (sender.ActiveTool.Equals(EraserTool))
            {
                InkRenderer.InkPresenter.InputProcessingConfiguration.Mode = InkInputProcessingMode.Erasing;
                _ActiveTool = CurrentTool.Eraser;
            }
            else if (sender.ActiveTool.Equals(CursorTool))
            {
                InkRenderer.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.None;
                _ActiveTool = CurrentTool.Cursor;
            }
        }

        private void InkQuantitySlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            double penSize = (CurrentUserSettings.Instance.SaveData.Atelier.PencilSize = e.NewValue);
            _GlobalDrawingAttributes.Size = new Size(penSize, penSize);
            InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
        }

        private void OpacitySlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            double newOpacityValue = (CurrentUserSettings.Instance.SaveData.Atelier.PencilOpacity = e.NewValue);
            if (_ActiveTool == CurrentTool.Pencil)
            {
                _GlobalDrawingAttributes.PencilProperties.Opacity = newOpacityValue;
                InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
            }
        }

        private void ColorsToolbar_ItemClick(object sender, ItemClickEventArgs e)
        {
            AdaptiveGridView gridView = sender as AdaptiveGridView;
            var container = gridView.ContainerFromItem(e.ClickedItem) as GridViewItem;
            var paletteColorButton = gridView.FindDescendant<Ellipse>();

            CurrentUserSettings.Instance.SaveData.Atelier.SelectedColorIndex = gridView.Items.IndexOf(e.ClickedItem);
            CurrentUserSettings.Instance.SaveData.Atelier.PencilColor =
                ColorPalette.ToColorData((_GlobalDrawingAttributes.Color = ColorPalette.ToSolidColor((e.ClickedItem as ColorPalette).AsColorData())));
            InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);

            if (gridView.Items.IndexOf(e.ClickedItem) == gridView.SelectedIndex)
            {
                Flyout attachedFlyout = (Flyout)FlyoutBase.GetAttachedFlyout(paletteColorButton);
                Microsoft.Toolkit.Uwp.UI.Controls.ColorPicker colorPicker = (Microsoft.Toolkit.Uwp.UI.Controls.ColorPicker)attachedFlyout.Content;
                colorPicker.Color = ColorPalette.ToSolidColor(CurrentUserSettings.Instance.SaveData.Atelier.PencilColor);

                FlyoutBase.ShowAttachedFlyout(paletteColorButton);
            }
        }

        private void OnKeyboardDelKey(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            var strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
            int selectedStrokes = strokes.Where(stroke => stroke.Selected).Count();

            for (int i = 0; i < selectedStrokes; ++i)
            {
                int index = _UndoRequests.FindLastIndex(request => request == UndoRedoRequest.Ink);
                if (index != -1)
                {
                    _UndoRequests.RemoveAt(index);
                }
            }
            InkRenderer.InkPresenter.StrokeContainer.DeleteSelected();

            OverlayCanvas.Children.Clear();
            _HasActiveSelection = false;

            // delete selected bitmaps if any
            int layerObjCount = ObjectLayer.Children.Count;
            for (int i = 0; i < layerObjCount; ++i)
            {
                DesignerItem refToItem = (ObjectLayer.Children[i] as DesignerItem);
                if (refToItem != null && refToItem.IsSelected)
                {
                    ObjectLayer.Children.RemoveAt(i);
                    int reqBmpIndex = _UndoRequests.FindLastIndex(request => request == UndoRedoRequest.Bitmap);
                    if (reqBmpIndex != -1) _UndoRequests.RemoveAt(reqBmpIndex);
                }
            }
            SelectionCommandBar.Visibility = Visibility.Collapsed;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Define manifest data
            await DefineManifestData(e.Parameter as TagManifestData);

            // Define save data
            CurrentDrawingSession.Instance.SaveData = await CurrentDrawingSession.GetCurrent(ManifestData.BoardPath);

            // Load data
            LoadUserData();

            // try to load ink data from active file
            byte[] data = Convert.FromBase64String(CurrentDrawingSession.Instance.SaveData.StrokeData);
            using (MemoryStream ms = new MemoryStream(data))
            {
                if (ms.Length > 0)
                {
                    using (IInputStream inputStream = ms.AsInputStream())
                    {
                        await InkRenderer.InkPresenter.StrokeContainer.LoadAsync(inputStream);
                    }
                }
            }
            IReadOnlyList<InkStroke> strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
            for (int i = 0; i < strokes.Count - 1; ++i)
            {
                strokes[i].PointTransform = CurrentDrawingSession.Instance.SaveData.ActualScaling[i];
            }

            // load bitmap data
            foreach (BitmapData bitmapData in CurrentDrawingSession.Instance.SaveData.BitmapData)
            {
                byte[] pixelData = Convert.FromBase64String(bitmapData.RawData);

                WriteableBitmap wb = new WriteableBitmap((int)bitmapData.PixelWidth, (int)bitmapData.PixelHeight);
                using (Stream stream = wb.PixelBuffer.AsStream())
                {
                    await stream.WriteAsync(pixelData, 0, pixelData.Length);
                }

                Image image = new Image();
                image.Source = wb;
                image.Stretch = Stretch.Fill;

                DesignerItem designerItem = new DesignerItem(image);
                designerItem.ManualThumbsAdjust(bitmapData.PositionX, bitmapData.PositionY);
                designerItem.UpdateSize(bitmapData.Width, bitmapData.Height);
                designerItem.TransformMatrix = bitmapData.Transform;
                
                ObjectLayer.Children.Add(designerItem);
            }
        }

        private async Task DefineManifestData(TagManifestData manifest)
        {
            if (string.IsNullOrWhiteSpace(manifest.Id) || manifest.Id == null)
            {
                // Initialize manifest
                string guid = Guid.NewGuid().ToString();
                CurrentDrawingSession.Instance.SaveData = await CurrentDrawingSession.GetCurrent($"{guid}.json");
                CurrentDrawingSession.Instance.SaveData.Guid = guid;
                manifest.BoardPath = $"{guid}.json";
                manifest.Id = guid;
                ManifestData = manifest;

                // Update manifest list
                List<TagManifestData> dataList = await TagManifestManager.Instance.Get();
                dataList.Add(manifest);
                await TagManifestManager.Instance.Update(dataList);
            }
            else
            {
                ManifestData = await TagManifestManager.Instance.GetFromGuid(manifest.Id);
            }

            List<string> globalTags = await TagManagerService.Instance.GetGlobalTags();
            foreach (string tag in globalTags)
            {
                _TagsLinker.Add(new TagsLinker(tag, ManifestData.Tags.Any(t => t.Equals(tag))));
            }
        }

        private void LoadUserData()
        {
            int count = CurrentUserSettings.Instance.SaveData.Atelier.Palette.Count;
            if (count <= 0)
            {
                // fill palette with default values
                CurrentUserSettings.Instance.SaveData.Atelier.Palette = new ObservableCollection<ColorPalette>()
                {
                    new ColorPalette(new ColorData(0, 0, 0)),
                    new ColorPalette(new ColorData(255, 255, 255)),
                    new ColorPalette(new ColorData(127, 127, 127)),
                    new ColorPalette(new ColorData(195, 195, 195)),
                    new ColorPalette(new ColorData(136, 0, 21)),
                    new ColorPalette(new ColorData(185, 122, 87)),
                    new ColorPalette(new ColorData(237, 28, 36)),
                    new ColorPalette(new ColorData(255, 174, 201)),
                    new ColorPalette(new ColorData(255, 127, 39)),
                    new ColorPalette(new ColorData(255, 201, 14)),
                    new ColorPalette(new ColorData(255, 242, 0)),
                    new ColorPalette(new ColorData(239, 228, 176)),
                    new ColorPalette(new ColorData(34, 177, 76)),
                    new ColorPalette(new ColorData(181, 230, 29)),
                    new ColorPalette(new ColorData(0, 162, 232)),
                    new ColorPalette(new ColorData(153, 217, 224)),
                    new ColorPalette(new ColorData(63, 72, 204)),
                    new ColorPalette(new ColorData(112, 146, 190)),
                    new ColorPalette(new ColorData(163, 73, 164)),
                    new ColorPalette(new ColorData(200, 191, 231))
                };
            }
            ColorsToolbar.ItemsSource = CurrentUserSettings.Instance.SaveData.Atelier.Palette;
            ColorsToolbar.SelectedIndex = CurrentUserSettings.Instance.SaveData.Atelier.SelectedColorIndex;

            _ActiveTool = CurrentUserSettings.Instance.SaveData.Atelier.LastTool;
            if (_ActiveTool == CurrentTool.Pencil)
            {
                _GlobalDrawingAttributes = InkDrawingAttributes.CreateForPencil();
                _GlobalDrawingAttributes.PencilProperties.Opacity = OpacitySlider.Value;
                SpaceyToastToolBar.ActiveTool = PencilTool;
            }
            else
            {
                _GlobalDrawingAttributes.PenTip = CurrentUserSettings.Instance.SaveData.Atelier.PencilShape;
                SpaceyToastToolBar.ActiveTool = BrushTool;
            }
            _GlobalDrawingAttributes.Color = ColorPalette.ToSolidColor(CurrentUserSettings.Instance.SaveData.Atelier.PencilColor);

            _GlobalDrawingAttributes.Size = _ActiveTool == CurrentTool.Pencil
                ? new Size(InkQuantitySlider.Value, InkQuantitySlider.Value)
                : new Size(BrushSizeSlider.Value, BrushSizeSlider.Value);

            Brush_SquareShape.IsChecked = (_GlobalDrawingAttributes.PenTip == PenTipShape.Rectangle);
            Brush_CircleShape.IsChecked = (_GlobalDrawingAttributes.PenTip == PenTipShape.Circle);

            Pen_IsPressureEnabled.IsOn = CurrentUserSettings.Instance.SaveData.Atelier.PenIsPressureEnabled;
            Pen_IsTiltEnabled.IsOn = CurrentUserSettings.Instance.SaveData.Atelier.PenIsTiltEnabled;

            InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Load strokes in memory and write to the config file
            using (MemoryStream ms = new MemoryStream())
            {
                IReadOnlyList<InkStroke> inkStrokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();

                using (IOutputStream output = ms.AsOutputStream())
                {
                    await InkRenderer.InkPresenter.StrokeContainer.SaveAsync(output);
                }

                byte[] data = ms.ToArray();
                CurrentDrawingSession.Instance.SaveData.StrokeData = Convert.ToBase64String(data);
            }
            var strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
            CurrentDrawingSession.Instance.SaveData.ActualScaling = new List<Matrix3x2>();
            for (int i = 0; i < strokes.Count - 1; ++i)
            {
                CurrentDrawingSession.Instance.SaveData.ActualScaling.Add(strokes[i].PointTransform);
            }

            // save bitmap
            CurrentDrawingSession.Instance.SaveData.BitmapData = new List<BitmapData>();
            foreach (UIElement element in ObjectLayer.Children)
            {
                WriteableBitmap wb = ((element as DesignerItem).ItemContent as Image).Source as WriteableBitmap;
                byte[] pixelData;
                using (Stream stream = wb.PixelBuffer.AsStream())
                {
                    pixelData = new byte[(uint)stream.Length];
                    await stream.ReadAsync(pixelData, 0, pixelData.Length);
                }

                GeneralTransform t = element.TransformToVisual(null);
                System.Numerics.Vector3 position = element.ActualOffset;

                Rect rect = (element as DesignerItem).GetLocation();

                CurrentDrawingSession.Instance.SaveData.BitmapData.Add(new BitmapData(Convert.ToBase64String(pixelData),
                    wb.PixelWidth, wb.PixelHeight, rect.Width, rect.Height, rect.X, rect.Y, element.TransformMatrix));
            }
            // render and save thumbnail
            RenderTargetBitmap thumbnail = new RenderTargetBitmap();
            await thumbnail.RenderAsync(DrawingView, 256, 144);

            DisplayInformation displayInformation = DisplayInformation.GetForCurrentView();
            IBuffer buff = await thumbnail.GetPixelsAsync();
            byte[] pixelsData = buff.ToArray();

            // save thumbnail
            StorageFile thumbnailFile = (StorageFile)await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync($"{ManifestData.Id}.jpeg");
            if (thumbnailFile == null)
            {
                thumbnailFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"{ManifestData.Id}.jpeg");
            }
            using (var stream = await thumbnailFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                    256, 144,
                    displayInformation.RawDpiX,
                    displayInformation.RawDpiY,
                    pixelsData);
                await encoder.FlushAsync();
                stream.Seek(0);
            }

            List<string> newTags = new List<string>();

            foreach (var tags in _TagsLinker)
            {
                if (tags.IsAssigned) newTags.Add(tags.Name);
            }
            ManifestData.ThumbnailPath = thumbnailFile.Path;
            ManifestData.Tags = newTags;

            await TagManifestManager.Instance.UpdateTagsFromGuid(ManifestData.Tags, ManifestData.Id);
            await CurrentDrawingSession.Instance.UpdateLocalFile();
        }

        private void OnUndo(Windows.UI.Xaml.Input.KeyboardAccelerator sender, Windows.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            int count = _UndoRequests.Count;
            if (count <= 0)
                return;

            switch (_UndoRequests[count - 1])
            {
                case UndoRedoRequest.Ink:
                    List<InkStroke> selectedStrokesIDs = new List<InkStroke>();
                    IReadOnlyList<InkStroke> strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
                    if (strokes.Count == 0)
                    {
                        return;
                    }

                    foreach (var s in strokes)
                    {
                        if (s.Selected)
                        {
                            selectedStrokesIDs.Add(s);
                            s.Selected = false;
                        }
                    }

                    InkStroke inkStroke = strokes.Last();
                    inkStroke.Selected = true;
                    InkRenderer.InkPresenter.StrokeContainer.DeleteSelected();

                    // restore ink's position
                    int strokesCount = InkRenderer.InkPresenter.StrokeContainer.GetStrokes().Count;

                    foreach (var s in selectedStrokesIDs)
                    {
                        s.Selected = true;
                    }
                    break;
                case UndoRedoRequest.Bitmap:
                    ObjectLayer.Children.RemoveAt(ObjectLayer.Children.Count - 1);
                    break;
            }
            _UndoRequests.RemoveAt(count - 1);
        }

        private void BrushSizeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            double brushSize = (CurrentUserSettings.Instance.SaveData.Atelier.BrushSize = e.NewValue);
            _GlobalDrawingAttributes.Size = new Size(brushSize, brushSize);
            InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)(sender as RadioButton).IsChecked && _ActiveTool == CurrentTool.Brush)
            {
                _GlobalDrawingAttributes.PenTip = (CurrentUserSettings.Instance.SaveData.Atelier.PencilShape = PenTipShape.Circle);
                InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
            }
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            if ((bool)(sender as RadioButton).IsChecked && _ActiveTool == CurrentTool.Brush)
            {
                _GlobalDrawingAttributes.PenTip = (CurrentUserSettings.Instance.SaveData.Atelier.PencilShape = PenTipShape.Rectangle);
                InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
            }
        }

        private void Pen_IsPressureEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            bool isPressureEnabled = (sender as ToggleSwitch).IsOn;
            if (isPressureEnabled && _ActiveTool == CurrentTool.Pencil)
            {
                _GlobalDrawingAttributes.IgnorePressure = (CurrentUserSettings.Instance.SaveData.Atelier.PenIsPressureEnabled =
                    isPressureEnabled);
                InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
            }
        }

        private void Pen_IsTiltEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            bool isTiltEnabled = (sender as ToggleSwitch).IsOn;
            if (isTiltEnabled && _ActiveTool == CurrentTool.Pencil)
            {
                _GlobalDrawingAttributes.IgnorePressure = (CurrentUserSettings.Instance.SaveData.Atelier.PenIsTiltEnabled =
                    isTiltEnabled);
                InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.Tag == null)
            {
                return;
            }

            _TagsLinker.Where(t => t.Name == checkBox.Tag.ToString()).FirstOrDefault().IsAssigned = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox.Tag == null)
            {
                return;
            }

            _TagsLinker.Where(t => t.Name == checkBox.Tag.ToString()).FirstOrDefault().IsAssigned = false;
        }

        private async void AddTagsTextBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string userInput = (sender as TextBox).Text;
                var currentTags = await TagManagerService.Instance.GetGlobalTags();

                if (string.IsNullOrEmpty(userInput) || string.IsNullOrWhiteSpace(userInput)
                    || currentTags.Any(t => t == userInput) || userInput.Length > 32)
                {
                    return;
                }
                currentTags.Add(userInput);
                await TagManagerService.Instance.UpdateGlobalTags(currentTags);
                _TagsLinker.Add(new TagsLinker(currentTags.Last(), true));
                AddTagsTextBox.Text = string.Empty;
            }
        }

        private void OnColorPickerColorChanged(Microsoft.UI.Xaml.Controls.ColorPicker sender, Microsoft.UI.Xaml.Controls.ColorChangedEventArgs args)
        {
            int index = ColorsToolbar.SelectedIndex;
            if (index < 0)
                return;
            ColorData colorInfo = ColorPalette.ToColorData(args.NewColor);

            CurrentUserSettings.Instance.SaveData.Atelier.PencilColor = colorInfo;
            CurrentUserSettings.Instance.SaveData.Atelier.Palette[index].RawData = colorInfo;

            _GlobalDrawingAttributes.Color = args.NewColor;
            InkRenderer.InkPresenter.UpdateDefaultDrawingAttributes(_GlobalDrawingAttributes);
        }

        private void OnCopyCommand(object sender, RoutedEventArgs e)
        {
            InkRenderer.InkPresenter.StrokeContainer.CopySelectedToClipboard();

            if (InkRenderer.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                CommandBarPasteButton.IsEnabled = true;
            }
        }

        private void OnPasteCommand(object sender, RoutedEventArgs e)
        {
            if (!InkRenderer.InkPresenter.StrokeContainer.CanPasteFromClipboard())
                return;

            // paste at the center of the current view
            Point elementPosition = new Point(0, 0);
            elementPosition.X = DrawingView.HorizontalOffset + (DrawingView.ViewportWidth / 2) - (_SelectionBoundingRect.Width / 2);
            elementPosition.Y = DrawingView.VerticalOffset + (DrawingView.ViewportHeight / 2) - (_SelectionBoundingRect.Height / 2);

            int addedStrokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes().Count;
            _SelectionBoundingRect = InkRenderer.InkPresenter.StrokeContainer.PasteFromClipboard(elementPosition);
            addedStrokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes().Count - addedStrokes;

            OverlayCanvas.Children.Clear();

            // select the new element
            var strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
            foreach (var stroke in strokes)
            {
                stroke.Selected = false;
            }
            for (int i = 0; i < addedStrokes; ++i)
            {
                strokes[(strokes.Count - 1) - i].Selected = true;
            }

            // draw bounds
            Rectangle rcActiveSelection = new Rectangle()
            {
                Stroke = new SolidColorBrush(Windows.UI.Colors.Magenta),
                StrokeThickness = 2,
                Width = _SelectionBoundingRect.Width,
                Height = _SelectionBoundingRect.Height,
            };
            Canvas.SetLeft(rcActiveSelection, _SelectionBoundingRect.X);
            Canvas.SetTop(rcActiveSelection, _SelectionBoundingRect.Y);
            rcActiveSelection.IsHitTestVisible = false;

            _HasActiveSelection = true;

            OverlayCanvas.Children.Add(rcActiveSelection);

            CommandBarCopyButton.IsEnabled = true;
            CommandBarCutButton.IsEnabled = true;
            CommandBarPasteButton.IsEnabled = false;
            CommandBarDeleteButton.IsEnabled = true;
        }

        private void OnDeleteCommand(object sender, RoutedEventArgs e)
        {
            OnKeyboardDelKey(new KeyboardAccelerator(), null);
            if (InkRenderer.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                SelectionCommandBar.Visibility = Visibility.Visible;
                CommandBarDeleteButton.IsEnabled = false;
                CommandBarCopyButton.IsEnabled = false;
                CommandBarCutButton.IsEnabled = false;
            }
        }

        private void OnCutCommand(object sender, RoutedEventArgs e)
        {
            InkRenderer.InkPresenter.StrokeContainer.CopySelectedToClipboard();

            Rect tempBoundingRect = _SelectionBoundingRect;

            OnKeyboardDelKey(new KeyboardAccelerator(), null);

            _SelectionBoundingRect = tempBoundingRect;

            // Prevent the command bar from becoming collapsed
            SelectionCommandBar.Visibility = Visibility.Visible;
            CommandBarCutButton.IsEnabled = false;

            if (InkRenderer.InkPresenter.StrokeContainer.CanPasteFromClipboard())
            {
                CommandBarPasteButton.IsEnabled = true;
                CommandBarDeleteButton.IsEnabled = false;
            }
        }

        private void OverlayCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var currentPointer = e.GetCurrentPoint(InkRenderer);
            if (!currentPointer.Properties.IsLeftButtonPressed)
            {
                _lastPoint = currentPointer.RawPosition;
                _isAreaMoving = true;
            }
        }

        private void OverlayCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentPoint = e.GetCurrentPoint(InkRenderer).RawPosition;

            if (_isAreaMoving)
            {
                double deltaX = currentPoint.X - _lastPoint.X;
                double deltaY = currentPoint.Y - _lastPoint.Y;

                TransformViewport_Translate(new Vector2((float)(currentPoint.X - _lastPoint.X), 
                    (float)(currentPoint.Y - _lastPoint.Y)));
            }

            _lastPoint = currentPoint;
        }

        private void OverlayCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isAreaMoving = false;
        }

        private void OverlayCanvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if ((e.KeyModifiers & Windows.System.VirtualKeyModifiers.Control) != 0 && _ActiveTool == CurrentTool.Cursor)
            {
                var currentPoint = e.GetCurrentPoint(InkRenderer);
                float scaleFactor = currentPoint.Properties.MouseWheelDelta > 0 ? 1.1f : 0.9f;

                TransformViewport_Scale(scaleFactor, new Vector2((float)DrawingView.ActualWidth / 2, (float)DrawingView.ActualHeight / 2));
            }
        }

        // ManipulationDelta event - Use gesture to transform elements
        private void DrawingView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Vector2 origin = new Vector2((float)(DrawingView.ActualWidth / 2.0), (float)(DrawingView.ActualHeight / 2.0));
            Matrix3x2 translation = SpaceyToast.Source.Helpers.Transform.Translate(new Vector2((float)e.Delta.Translation.X, (float)e.Delta.Translation.Y));
            Matrix3x2 rotation = Matrix3x2.Identity;
            Matrix3x2 scale = 
                e.Cumulative.Scale < 0.3 ? 
                Matrix3x2.Identity : 
                e.Cumulative.Scale > 5.0 ? SpaceyToast.Source.Helpers.Transform.Scale(e.Delta.Scale, origin) : 
                Matrix3x2.Identity;

            Matrix4x4 translation4 = SpaceyToast.Source.Helpers.Transform.ConvertToMatrix4x4(translation);
            Matrix4x4 rotation4 = Matrix4x4.Identity;
            Matrix4x4 scale4 = SpaceyToast.Source.Helpers.Transform.ConvertToMatrix4x4(scale);

            IReadOnlyList <InkStroke> strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();
            for (int i = 0; i < strokes.Count - 1; ++i)
            {
                var stroke = strokes[i];
                stroke.PointTransform *= scale * rotation * translation;
            }
            _SelectionBoundingRect = InkRenderer.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));

            for (int i = 0; i < ObjectLayer.Children.Count - 1; ++i)
            {
                ObjectLayer.Children[i].TransformMatrix *= scale4 * rotation4 * translation4;
            }

            for (int i = 0; i < OverlayCanvas.Children.Count; ++i)
            {
                UIElement elem = OverlayCanvas.Children[i];
                OverlayCanvas.Children[i].TransformMatrix *=
                    Matrix4x4.CreateTranslation((float)Canvas.GetLeft(elem), (float)Canvas.GetTop(elem), 0.0f) *
                    scale4 *
                    Matrix4x4.CreateTranslation((float)-Canvas.GetLeft(elem), (float)-Canvas.GetTop(elem), 0.0f);
            }
        }

        void TransformViewport_Translate(Vector2 newPosition)
        {
            IReadOnlyList<InkStroke> strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();

            Matrix3x2 translationMatrix = SpaceyToast.Source.Helpers.Transform.Translate(newPosition);
            Matrix4x4 translationMatrixEx = SpaceyToast.Source.Helpers.Transform.ConvertToMatrix4x4(translationMatrix);
            
            for (int i = 0; i < strokes.Count; ++i)
            {
                strokes[i].PointTransform *= translationMatrix;
            }
            for (int i = 0; i < ObjectLayer.Children.Count; ++i)
            {
                ObjectLayer.Children[i].TransformMatrix *= translationMatrixEx;
            }
            for (int i = 0; i < OverlayCanvas.Children.Count; ++i)
            {
                OverlayCanvas.Children[i].TransformMatrix *= translationMatrixEx;
            }
            _SelectionBoundingRect = InkRenderer.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));
        }

        void TransformViewport_Scale(float scaleFactor, Vector2 origin)
        {
            IReadOnlyList<InkStroke> strokes = InkRenderer.InkPresenter.StrokeContainer.GetStrokes();

            Matrix3x2 scaleMatrix = SpaceyToast.Source.Helpers.Transform.Scale(scaleFactor, origin);
            if (scaleMatrix.IsIdentity) 
                return;

            Matrix4x4 scaleMatrixEx = SpaceyToast.Source.Helpers.Transform.ConvertToMatrix4x4(scaleMatrix);

            for (int i = 0; i < strokes.Count; ++i)
            {
                strokes[i].PointTransform *= scaleMatrix;
            }

            for (int i = 0; i < ObjectLayer.Children.Count; ++i)
            {
                ObjectLayer.Children[i].TransformMatrix *= scaleMatrixEx;
            }
            _SelectionBoundingRect = InkRenderer.InkPresenter.StrokeContainer.MoveSelected(new Point(0, 0));

            for (int i = 0; i < OverlayCanvas.Children.Count; ++i)
            {
                UIElement elem = OverlayCanvas.Children[i];
                OverlayCanvas.Children[i].TransformMatrix *= 
                    Matrix4x4.CreateTranslation((float)Canvas.GetLeft(elem), (float)Canvas.GetTop(elem), 0.0f) *
                    scaleMatrixEx *
                    Matrix4x4.CreateTranslation((float)-Canvas.GetLeft(elem), (float)-Canvas.GetTop(elem), 0.0f);
            }
        }

        // Note that the deleted tags are still linked to their corresponding notes until the next restart.
        private async void DeleteTagButtonClicked(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(sender as Button);
            if (parent == null) { return; }

            int selectedItem = AddTagsHost.GetElementIndex(parent as UIElement);
            string itemName = _TagsLinker.ElementAt(selectedItem).Name;

            var currentTags = await TagManagerService.Instance.GetGlobalTags();
            
            // safely get the corresponding index, even if it should be the same that the one assigned to _TagsLinker
            int globalTagIndex = currentTags.FindIndex(0, currentTags.Count, (str) => str == itemName);
            if (globalTagIndex == -1) return;
            currentTags.RemoveAt(globalTagIndex);
            await TagManagerService.Instance.UpdateGlobalTags(currentTags);

            _TagsLinker.RemoveAt(selectedItem);
        }
    }
}