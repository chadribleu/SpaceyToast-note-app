using Newtonsoft.Json;
using SpaceyToast.Models;
using System.Collections.ObjectModel;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;

namespace SpaceyToast.Source.User
{
    public enum StartingPage { CalendarPage, TagsPage, DrawingArea }
    public enum CurrentTheme { Light, Dark, System }
    public enum CurrentTool
    {
        Brush, Pencil, Eraser, SquareSelection,
        CircleSelection, FreeSelection, AddBitmap, Cursor, None
    }

    public class ColorData
    {
        public byte r, g, b;

        public ColorData(byte red, byte green, byte blue)
        {
            r = red;
            g = green;
            b = blue;
        }
    }

    public class Atelier
    {
        [JsonProperty("last_tool")]
        public CurrentTool LastTool = CurrentTool.Pencil;

        [JsonProperty("pen_shape")]
        public PenTipShape PencilShape = PenTipShape.Circle;

        [JsonProperty("zoom_factor")]
        public double ZoomFactor = 1.0;

        [JsonProperty("pen_size")]
        public double PencilSize = 5.0;

        [JsonProperty("brush_size")]
        public double BrushSize = 5.0;

        [JsonProperty("pen_opacity")]
        public double PencilOpacity = 1.0;

        [JsonProperty("color")]
        public ColorData PencilColor = new ColorData(0, 0, 0);

        [JsonProperty("palette_data")]
        public ObservableCollection<ColorPalette> Palette = new ObservableCollection<ColorPalette>();

        [JsonProperty("last_color_index")]
        public int SelectedColorIndex = 0;

        [JsonProperty("pen_is_pressure_enabled")]
        public bool PenIsPressureEnabled = true;

        [JsonProperty("pen_is_tilt_enabled")]
        public bool PenIsTiltEnabled = true;
    }

    public class CurrentCalendar
    {
        [JsonProperty("cb_selected_index")]
        public int ComboBoxSelectedIndex = 0;

        [JsonProperty("max_items")]
        public int MaxItems = 15;

        [JsonProperty("per_cycle_year_range")]
        public int PerCycleYearRange = 5;

        [JsonProperty("on_first_launch_year")]
        public int OnFirstLaunchYear = -1;

        [JsonProperty("starting_year_cycle")]
        public int StartingYearCycle = -1;

        [JsonProperty("current_year")]
        public int CurrentYear = -1;

        [JsonProperty("last_day")]
        public int LastDay = -1;

        [JsonProperty("last_month")]
        public int LastMonth = -1;
    }

    public class CurrentUserData
    {
        [JsonProperty("is_first_time")]
        public bool IsFirstTime { get; internal set; }

        [JsonProperty("starting_page")]
        public StartingPage StartingPage = StartingPage.CalendarPage;

        [JsonProperty("theme")]
        public ElementTheme CurrentTheme = ElementTheme.Default;

        [JsonProperty("atelier")]
        public Atelier Atelier = new Atelier();

        [JsonProperty("calendar")]
        public CurrentCalendar CurrentCalendar = new CurrentCalendar();
    }
}
