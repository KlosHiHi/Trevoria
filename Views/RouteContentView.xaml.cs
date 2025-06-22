namespace Trevoria.Views;

public partial class RouteContentView : ContentView
{
    public string RouteType;
    public string MovementType;
    public string PlaceTo;
    public string PlaceFrom;

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
        RadioButton button = sender as RadioButton;
        RouteType = _routes[Convert.ToInt32(button.Value)];
    }

    private void MovementRadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        RadioButton button = sender as RadioButton;
        MovementType = _movements[Convert.ToInt32(button.Value)];
    }
}