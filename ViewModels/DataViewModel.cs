using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class DataViewModel : BaseViewModel
    {
        public string MenuLogout => LocalizationService.Instance["Logout"];
    }
}
