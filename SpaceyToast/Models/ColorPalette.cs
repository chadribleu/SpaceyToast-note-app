using Newtonsoft.Json;
using SpaceyToast.Source.User;
using SpaceyToast.ViewModels;
using System;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SpaceyToast.Models
{
    public class ColorPalette : ViewModelBase
    {
        private ColorData _rawData;

        [JsonProperty("raw_data")]
        public ColorData RawData
        {
            get {  return _rawData; }
            set
            {
                _rawData = value;
                FillColor = new SolidColorBrush(Color.FromArgb(255, value.r, value.g, value.b));
                NotifyPropertyChanged(nameof(RawData));
            }
        }

        [JsonIgnore]
        private SolidColorBrush _borderColor;
        [JsonIgnore]
        public SolidColorBrush BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                NotifyPropertyChanged(nameof(BorderColor));
            }
        }
        [JsonIgnore]
        private SolidColorBrush _fillColor;
        [JsonIgnore]
        public SolidColorBrush FillColor
        {
            get { return _fillColor; }
            set
            {
                _fillColor = value;
                BorderColor = GetDarkerColorFromBrush(_fillColor);
                NotifyPropertyChanged(nameof(FillColor));
            }
        }

        public ColorPalette(ColorData color, uint shadeFactor = 50)
        {
            if (color != null)
            {
                RawData = color;
            }
        }

        public ColorData AsColorData()
        {
            return RawData;
        }

        public SolidColorBrush GetDarkerColorFromBrush(SolidColorBrush brush, uint percentage = 50)
        {
            double factor = System.Convert.ToDouble(percentage);

            byte newR = Convert.ToByte(brush.Color.R * (factor / 100.0));
            byte newG = Convert.ToByte(brush.Color.G * (factor / 100.0));
            byte newB = Convert.ToByte(brush.Color.B * (factor / 100.0));

            return new SolidColorBrush(Color.FromArgb(255, newR, newG, newB));
        }

        public static Color ToSolidColor(ColorData color)
        {
            return Color.FromArgb(255, color.r, color.g, color.b);
        }

        public static ColorData ToColorData(Color color)
        {
            return new ColorData(color.R, color.G, color.B);
        }
    }
}