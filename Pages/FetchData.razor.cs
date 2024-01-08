using Microsoft.AspNetCore.Components;

namespace BlazeApp.Pages
{
    public partial class FetchData
    {
        private List<CityInfo>? allCities;
        private List<CityInfo>? filteredCities;
        private DateTime selectedDate;

        protected override async Task OnInitializedAsync()
        {
            allCities = await CityService.GetAllCityInfosAsync();
            filteredCities = allCities;
        }

        void FilterRowsByDate(DateTime selectedDate)
        {
            filteredCities = allCities.Where(city => city.DateAdded.Date == selectedDate.Date).ToList();
        }

        void ResetCities()
        {
            filteredCities = allCities;
        }

        private void OnDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParse(e.Value?.ToString(), out DateTime newSelectedDate))
            {
                selectedDate = newSelectedDate;
                FilterRowsByDate(selectedDate);
            }
            else
            {
                ResetCities();
            }

            StateHasChanged();
        }
    }
}
