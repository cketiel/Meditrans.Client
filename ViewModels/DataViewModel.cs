﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class DataViewModel : BaseViewModel
    {
        #region Translation

        public string Customers => LocalizationService.Instance["Customers"];
        public string Scheduling => LocalizationService.Instance["Scheduling"];
        public string Location => LocalizationService.Instance["Location"];
        public string Other => LocalizationService.Instance["Other"];

        #endregion
    }
}
