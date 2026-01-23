using CommunityToolkit.Mvvm.ComponentModel;
using Meditrans.Client.DTOs;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using System.Collections.ObjectModel;

namespace Meditrans.Client.ViewModels
{
    public partial class TripHistoryViewModel : ObservableObject
    {
        private readonly TripHistoryService _historyService;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ObservableCollection<TripHistoryCreateDto> _historyRecords = new();
        [ObservableProperty] private int _tripId;
        [ObservableProperty] private string _patientName;
        [ObservableProperty] private string _pickupAddress;
        [ObservableProperty] private string _dropoffAddress;

        public TripHistoryViewModel(TripReadDto trip)
        {
            _tripId = trip.Id;
            _patientName = trip.CustomerName;
            _pickupAddress = trip.PickupAddress;
            _dropoffAddress = trip.DropoffAddress;

            _historyService = new TripHistoryService();
            LoadHistory();
        }

        private async void LoadHistory()
        {
            IsLoading = true;
            try
            {
                var records = await _historyService.GetHistoryByTripAsync(TripId);
                HistoryRecords = new ObservableCollection<TripHistoryCreateDto>(records);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}