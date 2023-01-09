using Newtonsoft.Json;
using System.Collections.Generic;
using System.Numerics;

namespace SpaceyToast.Source.User
{
    public class BitmapData
    {
        public BitmapData(string pixels, double pixelWidth, double pixelHeight, double width, double height, double x, double y, Matrix4x4 transform)
        {
            RawData = pixels;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
            Width = width;
            Height = height;
            PositionX= x;
            PositionY= y;
            Transform = transform;
        }

        [JsonProperty("raw_data")]
        public string RawData = "";

        [JsonProperty("transform_width")]
        public double Width = 8.0;

        [JsonProperty("transform_height")]
        public double Height = 8.0;

        [JsonProperty("transform_xpos")]
        public double PositionX = 0.0;

        [JsonProperty("transform_ypos")]
        public double PositionY = 0.0;

        [JsonProperty("pixel_width")]
        public double PixelWidth = 8.0;

        [JsonProperty("pixel_height")]
        public double PixelHeight = 8.0;

        [JsonProperty("transform_matrix")]
        public Matrix4x4 Transform = Matrix4x4.Identity;
    }

    public class BoardData
    {
        [JsonIgnore]
        public string ActiveFile = string.Empty;

        [JsonProperty("guid")]
        public string Guid = string.Empty;

        [JsonProperty("strokes_data")]
        public string StrokeData = string.Empty;

        [JsonProperty("bitmap_data")]
        public List<BitmapData> BitmapData = new List<BitmapData>();

        [JsonProperty("strokes_transform")]
        public List<Matrix3x2> ActualScaling = new List<Matrix3x2>();
    }
}
