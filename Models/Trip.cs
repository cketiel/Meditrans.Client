﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meditrans.Client.Models
{
    public class Trip
    {
        public Guid Id { get; set; }
        public string? Day { get; set; }
        public string? Date { get; set; }
        public string? FromTime { get; set; }
        public string? ToTime { get; set; }
        public string? PatientName { get; set; }
        public string? PickupAddress { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public string? DropoffAddress { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public string? SpaceType { get; set; }
        public string? PickupNote { get; set; }
        public bool IsCancelled { get; set; }
    }
}
