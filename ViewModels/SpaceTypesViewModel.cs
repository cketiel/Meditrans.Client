using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Meditrans.Client.Commands;
using Meditrans.Client.Models;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class SpaceTypesViewModel: BaseViewModel
    {
        #region Translation

        public string SpaceTypeNameText => LocalizationService.Instance["SpaceTypeName"];
        public string DescriptionText => LocalizationService.Instance["Description"];
        public string LoadTimeText => LocalizationService.Instance["LoadTime"];
        public string UnloadTimeText => LocalizationService.Instance["UnloadTime"];
        public string CapacityTypeText => LocalizationService.Instance["CapacityType"];
        public string InactiveText => LocalizationService.Instance["Inactive"];

        // Actions
        public string AddSpaceTypeToolTip => LocalizationService.Instance["AddSpaceType"];
        public string DeleteSpaceTypeToolTip => LocalizationService.Instance["DeleteSpaceType"]; 

        #endregion

        public ObservableCollection<SpaceType> SpaceTypes { get; set; } = new();

        private SpaceType _selectedSpaceType;
        public SpaceType SelectedSpaceType
        {
            get => _selectedSpaceType;
            set
            {
                _selectedSpaceType = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand AddSpaceTypeCommand { get; }
        public ICommand DeleteSpaceTypeCommand { get; }

        public SpaceTypesViewModel() {

            AddSpaceTypeCommand = new RelayCommandObject(_ => AddSpaceType());
            DeleteSpaceTypeCommand = new RelayCommandObject(DeleteSpaceType, _ => SelectedSpaceType != null);
            _ = LoadDataAsync();

        }

        private async Task LoadDataAsync()
        {
           
            LoadSpaceTypesAsync();
        }

        public async Task LoadSpaceTypesAsync()
        {
            SpaceTypeService _spaceTypeService = new SpaceTypeService();
            var sources = await _spaceTypeService.GetSpaceTypesAsync();
            SpaceTypes.Clear();
            foreach (var source in sources)
            {
                source.ShowNameDescription = source.Description + " " + source.Name;
                SpaceTypes.Add(source);
            }

        }

        private void AddSpaceType() {
            MessageBox.Show("AddSpaceType");
        }

        private void DeleteSpaceType(object obj)
        {
            MessageBox.Show("DeleteSpaceType");
            
        }


    } // end class
}// end namespace
