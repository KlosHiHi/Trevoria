using System.Diagnostics;
using System.Text.Json;

namespace Trevoria.Views;

public partial class RouteContentView : ContentView
{
    public string RouteType = "empty";
    public string MovementType = "foot";
    public string PlaceTo;
    public string PlaceFrom;
    private List<OsmCity> LocalityTown;

    private bool _isRouteTypeSelected;
    private bool _isMovementTypeSelected;

    private Dictionary<int, string> _routes;
    private Dictionary<int, string> _movements;

    public event Action TypeChanged;

    public RouteContentView()
    {
        InitializeComponent();

        _routes = new()
        {
            {1, "empty"},
            {2, "nature"},
            {3, "art"},
            {4, "food"},
            {5, "history"},
        };

        _movements = new()
        {
            {1, "foot"},
            {2, "bike"},
            {3, "car"},
        };
        InitAsync();
    }

    private async void InitAsync()
    {
        LocalityTown = await GetArkhangelskCitiesFromOsmAsync();
    }
    private void Button_Clicked(object sender, EventArgs e)
    {
        PlaceTo = LocationToEntry.Text;
        PlaceFrom = LocationFromEntry.Text;
        TypeChanged?.Invoke();
    }

    private void ChangeDirectionButton_Clicked(object sender, EventArgs e)
    {
        var temp = LocationFromEntry.Text;
        LocationFromEntry.Text = LocationToEntry.Text;
        LocationToEntry.Text = temp;
    }

    private void RouteRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _isRouteTypeSelected = NatureRadio.IsChecked || ArtRadio.IsChecked || FoodRadio.IsChecked || HistoryRadio.IsChecked;
        RadioButton button = sender as RadioButton;
        RouteType = _routes[Convert.ToInt32(button.Value)];
        BuildRouteButton.IsEnabled = _isRouteTypeSelected && _isMovementTypeSelected;
    }

    private void MovementRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        _isMovementTypeSelected = FootRadio.IsChecked || BikeRadio.IsChecked || CarRadio.IsChecked;
        RadioButton button = sender as RadioButton;
        MovementType = _movements[Convert.ToInt32(button.Value)];
        BuildRouteButton.IsEnabled = _isRouteTypeSelected && _isMovementTypeSelected;
    }

    private void SuggestionsListFrom_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not OsmCity town)
        {
            SuggestionsListFrom.SelectedItem = null;
            return;
        }

        LocationFromEntry.Text = town.Name;
        SuggestionsListFrom.IsVisible = false;
        SuggestionsListFrom.SelectedItem = null;

        LocationFromFrame.HeightRequest = LocationFromEntry.Height > 0 ? LocationFromEntry.Height : 60;
        try
        {
            if (PlaceFrom == null)
                PlaceFrom = "";
            PlaceFrom = LocationFromEntry.Text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Îøèáêà: {ex.Message}");
        }
    }

    private void SuggestionsListTo_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem is not OsmCity town)
        {
            SuggestionsListTo.SelectedItem = null;
            return;
        }

        LocationToEntry.Text = town.Name;
        SuggestionsListTo.IsVisible = false;
        SuggestionsListTo.SelectedItem = null;

        LocationToFrame.HeightRequest = LocationToEntry.Height > 0 ? LocationToEntry.Height : 60;
        try
        {
            if (PlaceTo == null)
                PlaceTo = "";
            PlaceTo = LocationToEntry.Text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Îøèáêà: {ex.Message}");
        }
    }


    private void LocationToEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LocalityTown == null)
            return;
        var query = e.NewTextValue?.ToLower() ?? "";
        var suggestions = LocalityTown
            .Where(t => (t.Name ?? "").ToLower().Contains(query))
            .Take(5)
            .ToList();

        SuggestionsListTo.ItemsSource = suggestions;
        SuggestionsListTo.IsVisible = suggestions.Any();

        int visibleCount = suggestions.Count;
        SuggestionsListTo.HeightRequest = visibleCount * 44;

        double entryHeight = LocationToEntry.Height > 0 ? LocationToEntry.Height : 44;
        LocationToFrame.HeightRequest = entryHeight + SuggestionsListTo.HeightRequest;
        if (!suggestions.Any())
        {
            LocationToEntry.HeightRequest = LocationToEntry.Height + 16;
        }
        if (LocationToEntry.Text == string.Empty || visibleCount == 0)
        {
            LocationToFrame.HeightRequest = LocationToEntry.Height;
        }
    }

    private void LocationFromEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (LocalityTown == null)
            return;
        var query = e.NewTextValue?.ToLower() ?? "";
        var suggestions = LocalityTown
            .Where(t => (t.Name ?? "").ToLower().Contains(query))
            .Take(5)
            .ToList();

        SuggestionsListFrom.ItemsSource = suggestions;
        SuggestionsListFrom.IsVisible = suggestions.Any();

        int visibleCount = suggestions.Count;
        SuggestionsListFrom.HeightRequest = visibleCount * 44;

        double entryHeight = LocationFromEntry.Height > 0 ? LocationFromEntry.Height : 44;
        LocationFromFrame.HeightRequest = entryHeight + SuggestionsListFrom.HeightRequest;
        if (!suggestions.Any())
        {
            LocationFromFrame.HeightRequest = LocationFromEntry.Height + 16;
        }
        if (LocationFromEntry.Text == string.Empty || visibleCount == 0)
        {
            LocationFromFrame.HeightRequest = LocationFromEntry.Height;
        }
    }
    public async Task<List<OsmCity>> GetArkhangelskCitiesFromOsmAsync()
    {
        var cities = new List<OsmCity>();
        string overpassUrl = "https://overpass-api.de/api/interpreter";
        string query = """
    [out:json][timeout:25];
    area["ISO3166-2"="RU-ARK"]->.searchArea;
    (
      node["place"~"city|town"](area.searchArea);
    );
    out body;
    """;

        using var client = new HttpClient();
        var content = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("data", query)
    });
        var response = await client.PostAsync(overpassUrl, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("elements", out var elements))
            return cities;

        foreach (var el in elements.EnumerateArray())
        {
            if (el.TryGetProperty("tags", out var tags) && tags.TryGetProperty("name", out var nameProp))
            {
                string name = nameProp.GetString();
                double lat = el.GetProperty("lat").GetDouble();
                double lon = el.GetProperty("lon").GetDouble();
                cities.Add(new OsmCity { Name = name, Lat = lat, Lon = lon });
            }
        }
        return cities;
    }
}