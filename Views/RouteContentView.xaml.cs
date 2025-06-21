namespace Trevoria.Views;

public partial class RouteContentView : ContentView
{
    public string RouteType;
    private Dictionary<int, string> _routes;


    public RouteContentView()
	{
		InitializeComponent();

        _routes = new() 
        {
            {1, "nature"},
            {2, "art"},
            {3, "food"},
            {4, "history"},
        };
    }

    private void ChangeDirectionButton_Clicked(object sender, EventArgs e)
    {
        var temp = LocationFromEntry.Text;
        LocationFromEntry.Text = LocationToEntry.Text;
        LocationToEntry.Text = temp;
    }

    private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        RadioButton button = sender as RadioButton;
        RouteType = _routes[Convert.ToInt32(button.Value)];
    }
}