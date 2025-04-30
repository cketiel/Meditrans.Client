﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Services;

namespace Meditrans.Client.ViewModels
{
    public class SchedulingViewModel: BaseViewModel
    {
        #region Translation

        public string Runs => LocalizationService.Instance["Runs"];
        public string ManageVehicles => LocalizationService.Instance["ManageVehicles"];
        public string ViolationSets => LocalizationService.Instance["ViolationSets"];

        #endregion
    }
}
