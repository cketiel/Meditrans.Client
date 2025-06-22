using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class SchedulesTabControlViewModel : BaseViewModel
    {
        #region Translation

        public string Schedule => LocalizationService.Instance["Schedule"];
        public string Trips => LocalizationService.Instance["Trips"];
        public string Revenue => LocalizationService.Instance["Revenue"];
        public string Graphs => LocalizationService.Instance["Graphs"];

        #endregion
    }
}
