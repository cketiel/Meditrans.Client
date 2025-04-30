using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class AdminViewModel: BaseViewModel
    {
        #region Translation

        public string Employees => LocalizationService.Instance["Employees"];
        public string Billing => LocalizationService.Instance["Billing"];
        public string Profile => LocalizationService.Instance["Profile"];

        #endregion
    }
}
