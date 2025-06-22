using Trevoria.ViewModel;

namespace Trevoria.Views;

public partial class ProfileContentView : ContentView
{
    public ProfileContentView()
    {
        InitializeComponent();
        BindingContext = new ProfileViewModel();
    }

    private async void OnChangePhotoTapped(object sender, EventArgs e)
    {
        try
        {
            var result = await MediaPicker.PickPhotoAsync();
            if (result != null)
            {
                var stream = await result.OpenReadAsync();
                profileImage.Source = ImageSource.FromStream(() => stream);
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}