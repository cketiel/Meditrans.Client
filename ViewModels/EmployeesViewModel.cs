using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class EmployeesViewModel : BaseViewModel
    {
        #region Translation

        public string Users => LocalizationService.Instance["Users"];
        public string Roles => LocalizationService.Instance["Roles"];

        #endregion
    }
}
