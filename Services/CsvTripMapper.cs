﻿using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;

using System.Text.Json;
using Meditrans.Client.Models.Csv;
using Meditrans.Client.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using Meditrans.Client.Exceptions;
using System.Diagnostics;
using Meditrans.Client.DTOs;

namespace Meditrans.Client.Services
{
    /*public static class MobilityType
    {
        public static string Ambulatory = "Appointment";
        public static string Return = "Return";
    }*/
    
    public class CsvTripMapper
    {
        private readonly ObservableCollection<TripReadDto> _trips;
        private readonly ObservableCollection<SpaceType> _spaceTypes;
        private readonly ObservableCollection<CapacityType> _capacityTypes;
        private readonly ObservableCollection<Customer> _customers;
        private readonly ObservableCollection<FundingSource> _fundingSources;
        private readonly GoogleMapsService _googleMapsService;
        private readonly ISpaceTypeService _spaceTypeService;
        private readonly ICapacityTypeService _capacityTypeService;
        private readonly ICustomerService _customerService;
        private readonly IFundingSourceService _fundingSourceService;
        private readonly TripService _tripService;

        public CsvTripMapper(
            ObservableCollection<TripReadDto> trips,
            ObservableCollection<SpaceType> spaceTypes,
            ObservableCollection<CapacityType> capacityTypes,
            ObservableCollection<Customer> customers,
            ObservableCollection<FundingSource> fundingSources,
            GoogleMapsService googleMapsService, 
            ISpaceTypeService spaceTypeService,
            ICapacityTypeService capacityTypeService,
            ICustomerService customerService,
            IFundingSourceService fundingSourceService,
            TripService tripService)
        {
            _trips = trips;
            _spaceTypes = spaceTypes;
            _capacityTypes = capacityTypes;
            _customers = customers;
            _fundingSources = fundingSources;
            _googleMapsService = googleMapsService;
            _spaceTypeService = spaceTypeService;
            _capacityTypeService = capacityTypeService;
            _customerService = customerService;
            _fundingSourceService = fundingSourceService;
            _tripService = tripService; 
        }

        public async Task<TripReadDto> MapToTripAsyncOld(CsvTripRawModel raw, FundingSource fundingSource/*string fundingSourceName*/)
        {

            TripReadDto matchingTrip = _trips?
                .Where(t => t != null)
                .FirstOrDefault(t =>
                    string.Equals(t.TripId, raw.RideId, StringComparison.OrdinalIgnoreCase)
                );
            // 
            if (_trips == null || !_trips.Any() || matchingTrip == null)
            {
                // Trip list empty, null or no matches
                Console.WriteLine("No trips found or the trip list is empty.");

                var trip = new Trip();

                // 1. TripId
                trip.TripId = raw.RideId;

                // 2. Address and coordinates
                trip.PickupAddress = raw.FromSt;
                trip.DropoffAddress = raw.ToST;

                // They can only be made (50 calls per second, 3000 per minute) to the Google Maps API using a free api key.
                Coordinates pickupCoords = await GetCoordinates(raw.FromSt, raw.FromCity, raw.FromState, raw.FromZIP);
                trip.PickupLatitude = pickupCoords.Latitude;
                trip.PickupLongitude = pickupCoords.Longitude;

                // They can only be made (50 calls per second, 3000 per minute) to the Google Maps API using a free api key.
                Coordinates dropoffCoords = await GetCoordinates(raw.ToST, raw.ToCity, raw.ToState, raw.ToZip);
                trip.DropoffLatitude = dropoffCoords.Latitude;
                trip.DropoffLongitude = dropoffCoords.Longitude;

                // 3. SpaceType
                var spaceType = _spaceTypes.FirstOrDefault(st => st.Name.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));
                int spaceTypeId = 0;
                if (spaceType == null)
                {
                    var capacityType = _capacityTypes.FirstOrDefault(ct => ct.Name.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));
                    var newSpaceTypeData = new SpaceType
                    {
                        Name = raw.Type,
                        Description = raw.Type,
                        CapacityTypeId = capacityType?.Id ?? 0,
                        LoadTime = 0,
                        UnloadTime = 0,
                        IsActive = true
                    };
                    //_spaceTypes.Add(newSpaceTypeData); //
                    try
                    {
                        SpaceType spaceTypeCreated = await _spaceTypeService.CreateSpaceTypeAsync(newSpaceTypeData);
                        spaceTypeId = spaceTypeCreated?.Id ?? 0;
                    }
                    catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict ||
                                     (apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.ErrorDetails != null && apiEx.ErrorDetails.Contains("already exists")))
                    {
                        // The SpaceType probably already exists (created by another concurrent task). Try to get it.
                        Debug.WriteLine($"SpaceType '{raw.Type}' there already existed or there was a conflict. Trying to get it back. Status: {apiEx.StatusCode}, Detail: {apiEx.ErrorDetails}");
                        SpaceType existingSpaceType = await _spaceTypeService.GetSpaceTypeByNameAsync(raw.Type);
                        if (existingSpaceType == null)
                        {
                            // This is unexpected if the creation failed due to duplicate reasons. 
                            Debug.WriteLine($"CRITICAL: SpaceType creation failed '{raw.Type}' due to conflict/duplicate, but could not be recovered.");
                            throw new InvalidOperationException($"Could not create or retrieve SpaceType: {raw.Type}", apiEx);
                        }
                        spaceTypeId = existingSpaceType.Id;
                    }

                }
                else
                {
                    spaceTypeId = spaceType.Id;
                }

                trip.SpaceTypeId = spaceTypeId;// spaceType.Id;

                // 4. Distance
                trip.Distance = double.TryParse(raw.Distance, out var dist) ? dist : 0;

                // 5. Dates and Times
                trip.Date = ParseDate(raw.Date);
                trip.Day = trip.Date.DayOfWeek.ToString();
                trip.FromTime = ParseTime(raw.PickupTime);
                trip.ToTime = string.IsNullOrWhiteSpace(raw.Appointment) ? null : ParseTime(raw.Appointment);

                /*trip.Date = DateTime.ParseExact(raw.Date, "M/d/yyyy", CultureInfo.InvariantCulture);

                if (TimeSpan.TryParse(raw.PickupTime, out var pickupTime))
                    trip.FromTime = pickupTime;
                else if (DateTime.TryParseExact(raw.PickupTime, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var pickupDateTime))
                    trip.FromTime = pickupDateTime.TimeOfDay;

                if (DateTime.TryParseExact(raw.Appointment, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toTime))
                    trip.ToTime = toTime.TimeOfDay;*/

                // 6. Customer
                var fullName = $"{raw.PatientFirstName} {raw.PatientLastName}".Trim();
                var RiderIdBuilt = $"{fullName} {raw.PatientPhoneNumber}".Trim();
                var customer = _customers
                    .Where(c => c != null)
                    .FirstOrDefault(c =>
                        string.Equals(c.FullName, fullName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.Phone, raw.PatientPhoneNumber, StringComparison.OrdinalIgnoreCase)
                    );

                //var fundingSourceId = _fundingSources.FirstOrDefault(f => f.Name == fundingSourceName)?.Id ?? 0;
                var fundingSourceId = fundingSource.Id;
                int customerId = 0;
                if (customer == null)
                {
                    var RiderIdNewData = RiderIdBuilt;
                    if (string.IsNullOrEmpty(raw.RiderId))
                    {
                        RiderIdNewData = RiderIdBuilt;
                    }
                    else
                    {
                        RiderIdNewData = raw.RiderId;
                    }

                    var newCustomerData = new Customer
                    {
                        //RiderId = raw.RiderId==null ? raw.RiderId : RiderIdBuilt,
                        RiderId = RiderIdNewData,
                        FullName = fullName,
                        Phone = raw.PatientPhoneNumber,
                        MobilePhone = raw.AlternativePhoneNumber,
                        Address = raw.FromSt,
                        City = raw.FromCity,
                        State = raw.FromState,
                        Zip = raw.FromZIP,
                        FundingSourceId = fundingSourceId,
                        SpaceTypeId = spaceTypeId,
                        Gender = raw.Gender ?? "Male",
                        Created = DateTime.Now,
                        CreatedBy = SessionManager.Username
                    };

                    //_customers.Add(newCustomerData);
                    try
                    {
                        Customer customerCreated = await _customerService.CreateCustomerAsync(MapToCustomerCreateDto(newCustomerData));
                        customerId = customerCreated.Id;
                    }
                    catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict ||
                                     (apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.ErrorDetails != null && apiEx.ErrorDetails.Contains("already exists")))
                    {
                        // The Customer probably already exists (created by another concurrent task). Try to get it.
                        Debug.WriteLine($"Customer '{raw?.RiderId}' there already existed or there was a conflict. Trying to get it back. Status: {apiEx.StatusCode}, Detail: {apiEx.ErrorDetails}");
                        Customer existingCustomer = await _customerService.GetCustomerByRiderIdAsync(newCustomerData.RiderId);
                        if (existingCustomer == null)
                        {
                            // This is unexpected if the creation failed due to duplicate reasons. 
                            Debug.WriteLine($"CRITICAL: Customer creation failed '{newCustomerData.RiderId}' due to conflict/duplicate, but could not be recovered.");
                            throw new InvalidOperationException($"Could not create or retrieve Customer: {newCustomerData.RiderId}", apiEx);
                        }
                        customerId = existingCustomer.Id;
                    }
                    //Falta
                    //Evitar duplicado de Customers, primero tengo que establecer CustomerId unico en BD de la api para capturar el error.
                    //catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict ||
                    //(apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.ErrorDetails != null && apiEx.ErrorDetails.Contains("already exists")))
                    /*catch (ApiException ex)
                    {
                        MessageBox.Show(
                            $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                            "Error del servidor",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error inesperado: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        
                    }*/

                }
                else
                {
                    customerId = customer.Id;
                }

                trip.CustomerId = customerId; // customer.Id;

                bool isWillCall = string.Equals(raw.Status, "WillCall", StringComparison.OrdinalIgnoreCase);

                // 7. Other fields
                trip.Type = isWillCall ? TripType.Return : TripType.Appointment;
                trip.Status = TripStatus.Accepted; // (Member may be notified.)
                trip.PickupComment = raw.AdditionalNotes;
                trip.DropoffComment = raw.Treatment;
                trip.PickupPhone = raw.PatientPhoneNumber;
                trip.Pickup = fullName + " " + "Pickup - " + trip.Type;
                trip.Dropoff = fullName + " " + "Dropoof - " + trip.Type;
                trip.WillCall = isWillCall;

                // 8. FundingSource
                trip.FundingSourceId = fundingSourceId;//_fundingSources.FirstOrDefault(f => f.Name == fundingSourceName)?.Id ?? 0;

                // 9. Creation date
                trip.Created = DateTime.Now;

                try
                {
                    TripReadDto tripCreated = await _tripService.CreateTripAsync(trip);
                    return tripCreated;
                }
                catch (ApiException ex)
                {
                    MessageBox.Show(
                        $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                        "Error del servidor",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new InvalidOperationException($"No se pudo crear ni recuperar Trip: {raw.RideId}", ex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error inesperado: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new InvalidOperationException($"No se pudo crear ni recuperar Trip: {raw.RideId}", ex);
                }

                //return trip;
            }
            else
            {
                // Aqui va la logica para cancelar el viaje. Hay que verificar que sea cancelacion. o simplemente se intenta importar mas datos parciales de ese Funding source.
                return matchingTrip;
            }


        }
        public async Task<TripReadDto> MapToTripAsync(CsvTripRawModel raw, FundingSource fundingSource, bool isSaferide)
        {
            var allMobilityTypes = MobilityType.AllMobilityTypes();
            MobilityType mobilityType = new MobilityType();

            string fullName = string.Empty;
            string RiderIdBuilt = string.Empty;
            string tripType = string.Empty;
            bool isWillCall = false;

            TripReadDto matchingTrip = _trips?
                .Where(t => t != null)
                .FirstOrDefault(t =>
                    string.Equals(t.TripId, raw.RideId, StringComparison.OrdinalIgnoreCase)
                );
            // 
            if (_trips == null || !_trips.Any() || matchingTrip == null)
            {
                // Trip list empty, null or no matches
                Console.WriteLine("No trips found or the trip list is empty.");
                
                var trip = new Trip();

                // 1. TripId
                trip.TripId = raw.RideId;

                // 2. Address and coordinates
                trip.PickupAddress = raw.FromSt;
                trip.DropoffAddress = raw.ToST;

                if (isSaferide)
                {
                    // They can only be made (50 calls per second, 3000 per minute) to the Google Maps API using a free api key.
                    Coordinates pickupCoords = await GetCoordinates(raw.FromSt, raw.FromCity, raw.FromState, raw.FromZIP);
                    trip.PickupLatitude = pickupCoords.Latitude;
                    trip.PickupLongitude = pickupCoords.Longitude;

                    // They can only be made (50 calls per second, 3000 per minute) to the Google Maps API using a free api key.
                    Coordinates dropoffCoords = await GetCoordinates(raw.ToST, raw.ToCity, raw.ToState, raw.ToZip);
                    trip.DropoffLatitude = dropoffCoords.Latitude;
                    trip.DropoffLongitude = dropoffCoords.Longitude;

                    fullName = $"{raw.PatientFirstName} {raw.PatientLastName}".Trim();
                    RiderIdBuilt = $"{fullName} {raw.PatientPhoneNumber}".Trim();

                    isWillCall = string.Equals(raw.Status, "WillCall", StringComparison.OrdinalIgnoreCase);

                    mobilityType = allMobilityTypes.FirstOrDefault(mt => mt.SpaceType.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));

                }
                else 
                {
                    trip.PickupLatitude = double.TryParse(raw.PickupLatitude, out var plt) ? plt : 0;
                    trip.PickupLongitude = double.TryParse(raw.PickupLongitude, out var plg) ? plg : 0;
                    trip.DropoffLatitude = double.TryParse(raw.DropoffLatitude, out var dlt) ? dlt : 0;
                    trip.DropoffLongitude = double.TryParse(raw.DropoffLongitude, out var dlg) ? dlg : 0;

                    fullName = raw.PatientFullName;
                    RiderIdBuilt = raw.RiderId;

                    //string.IsNullOrEmpty
                    isWillCall = string.IsNullOrWhiteSpace(raw.Appointment) && string.IsNullOrWhiteSpace(raw.PickupTime);

                    mobilityType = allMobilityTypes.FirstOrDefault(mt => mt.Description.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));

                }


                // 3. SpaceType
                /*if (raw.Type.Equals("WheelChair", StringComparison.OrdinalIgnoreCase))
                {
                    raw.Type = "WCH";
                }
                else if (raw.Type.Equals("WBARIATRIC", StringComparison.OrdinalIgnoreCase))
                {
                    raw.Type = "BWCH";
                }*/

                var mobilityTypeName = mobilityType.SpaceType;

                //var spaceType = _spaceTypes.FirstOrDefault(st => st.Name.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));
                var spaceType = _spaceTypes.FirstOrDefault(st => st.Name.Equals(mobilityTypeName, StringComparison.OrdinalIgnoreCase));

                int spaceTypeId = 0;
                if (spaceType == null)
                {
                    var capacityName = mobilityTypeName;
                    if (mobilityTypeName.Equals("C2C", StringComparison.OrdinalIgnoreCase) || mobilityTypeName.Equals("D2D", StringComparison.OrdinalIgnoreCase)) 
                    {
                        capacityName = "AMB";
                    }
                    //var capacityType = _capacityTypes.FirstOrDefault(ct => ct.Name.Equals(raw.Type, StringComparison.OrdinalIgnoreCase));
                    var capacityType = _capacityTypes.FirstOrDefault(ct => ct.Name.Equals(capacityName, StringComparison.OrdinalIgnoreCase));
                    var newSpaceTypeData = new SpaceType
                    {
                        Name = mobilityType.SpaceType, // raw.Type,
                        Description = mobilityType.Description, // raw.Type,
                        CapacityTypeId = capacityType?.Id ?? 0,
                        LoadTime = 0,
                        UnloadTime = 0,
                        IsActive = true
                    };
                    //_spaceTypes.Add(newSpaceTypeData); //
                    try
                    {
                        SpaceType spaceTypeCreated = await _spaceTypeService.CreateSpaceTypeAsync(newSpaceTypeData);
                        spaceTypeId = spaceTypeCreated?.Id ?? 0;
                    }
                    catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict ||
                                     (apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.ErrorDetails != null && apiEx.ErrorDetails.Contains("already exists")))
                    {
                        // The SpaceType probably already exists (created by another concurrent task). Try to get it.
                        Debug.WriteLine($"SpaceType '{raw.Type}' there already existed or there was a conflict. Trying to get it back. Status: {apiEx.StatusCode}, Detail: {apiEx.ErrorDetails}");
                        SpaceType existingSpaceType = await _spaceTypeService.GetSpaceTypeByNameAsync(raw.Type); 
                        if (existingSpaceType == null)
                        {
                            // This is unexpected if the creation failed due to duplicate reasons. 
                            Debug.WriteLine($"CRITICAL: SpaceType creation failed '{raw.Type}' due to conflict/duplicate, but could not be recovered.");
                            throw new InvalidOperationException($"Could not create or retrieve SpaceType: {raw.Type}", apiEx);
                        }
                        spaceTypeId = existingSpaceType.Id;
                    }

                }
                else
                {
                    spaceTypeId = spaceType.Id;
                }

                trip.SpaceTypeId = spaceTypeId;// spaceType.Id;

                // 4. Distance
                trip.Distance = double.TryParse(raw.Distance, out var dist) ? dist : 0;

                // 5. Dates and Times
                trip.Date = ParseDate(raw.Date);
                trip.Day = trip.Date.DayOfWeek.ToString();
                trip.FromTime = string.IsNullOrWhiteSpace(raw.PickupTime) ? null : ParseTime(raw.PickupTime); 
                trip.ToTime = string.IsNullOrWhiteSpace(raw.Appointment) ? null : ParseTime(raw.Appointment);

                /*trip.Date = DateTime.ParseExact(raw.Date, "M/d/yyyy", CultureInfo.InvariantCulture);

                if (TimeSpan.TryParse(raw.PickupTime, out var pickupTime))
                    trip.FromTime = pickupTime;
                else if (DateTime.TryParseExact(raw.PickupTime, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var pickupDateTime))
                    trip.FromTime = pickupDateTime.TimeOfDay;

                if (DateTime.TryParseExact(raw.Appointment, "h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var toTime))
                    trip.ToTime = toTime.TimeOfDay;*/

                // 6. Customer
                //var fullName = $"{raw.PatientFirstName} {raw.PatientLastName}".Trim();
                //var RiderIdBuilt = $"{fullName} {raw.PatientPhoneNumber}".Trim();
                var customer = _customers
                    .Where(c => c != null)
                    .FirstOrDefault(c =>
                        string.Equals(c.FullName, fullName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.Phone, raw.PatientPhoneNumber, StringComparison.OrdinalIgnoreCase)
                    );               

                //var fundingSourceId = _fundingSources.FirstOrDefault(f => f.Name == fundingSourceName)?.Id ?? 0;
                var fundingSourceId = fundingSource.Id;
                int customerId = 0;
                if (customer == null)
                {
                    var RiderIdNewData = RiderIdBuilt;
                    /*if (string.IsNullOrEmpty(raw.RiderId))
                    {
                        RiderIdNewData = RiderIdBuilt;
                    }
                    else 
                    {
                        RiderIdNewData = raw.RiderId;
                    }*/

                    var newCustomerData = new Customer
                    {
                        //RiderId = raw.RiderId==null ? raw.RiderId : RiderIdBuilt,
                        RiderId = RiderIdNewData,
                        FullName = fullName,
                        Phone = raw.PatientPhoneNumber,
                        MobilePhone = raw.AlternativePhoneNumber,
                        Address = raw.FromSt,
                        City = raw.FromCity,
                        State = raw.FromState,
                        Zip = raw.FromZIP,
                        FundingSourceId = fundingSourceId,
                        SpaceTypeId = spaceTypeId,
                        Gender = raw.Gender ?? "Male",
                        DOB = ParseDate(raw.PatientDOB),
                        Created = DateTime.Now,
                        CreatedBy = SessionManager.Username
                    };

                    //_customers.Add(newCustomerData);
                    try
                    {
                        Customer customerCreated = await _customerService.CreateCustomerAsync(MapToCustomerCreateDto(newCustomerData));
                        customerId = customerCreated.Id;
                    }
                    catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Conflict ||
                                     (apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest && apiEx.ErrorDetails != null && apiEx.ErrorDetails.Contains("already exists")))
                    {
                        // The Customer probably already exists (created by another concurrent task). Try to get it.
                        Debug.WriteLine($"Customer '{raw?.RiderId}' there already existed or there was a conflict. Trying to get it back. Status: {apiEx.StatusCode}, Detail: {apiEx.ErrorDetails}");
                        Customer existingCustomer = await _customerService.GetCustomerByRiderIdAsync(newCustomerData.RiderId);
                        if (existingCustomer == null)
                        {
                            // This is unexpected if the creation failed due to duplicate reasons. 
                            Debug.WriteLine($"CRITICAL: Customer creation failed '{newCustomerData.RiderId}' due to conflict/duplicate, but could not be recovered.");
                            throw new InvalidOperationException($"Could not create or retrieve Customer: {newCustomerData.RiderId}", apiEx);
                        }
                        customerId = existingCustomer.Id;
                    }

                    // Error de concurrencia (DbUpdateConcurrencyException)
                    /*catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error inesperado: {ex.Message}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                    }*/

                }
                else
                {
                    customerId = customer.Id;
                }

                trip.CustomerId = customerId; // customer.Id;

                //bool isWillCall = string.Equals(raw.Status, "WillCall", StringComparison.OrdinalIgnoreCase);
                //trip.Type = isWillCall ? TripType.Return : TripType.Appointment;
                // 7. Other fields
                tripType = isWillCall ? TripType.Return : TripType.Appointment;
                trip.Type = tripType;
                trip.Status = TripStatus.Accepted; // (Member may be notified.)
                trip.PickupComment = raw.AdditionalNotes;
                trip.DropoffComment = raw.Treatment;
                trip.PickupPhone = raw.PatientPhoneNumber;
                trip.DropoffPhone = raw.DropoffPhone;
                trip.Pickup = fullName + " " + "Pickup - " + trip.Type;
                trip.Dropoff = fullName + " " + "Dropoof - " + trip.Type;
                trip.WillCall = isWillCall;

                // 8. FundingSource
                trip.FundingSourceId = fundingSourceId;//_fundingSources.FirstOrDefault(f => f.Name == fundingSourceName)?.Id ?? 0;

                // 9. Creation date
                trip.Created = DateTime.Now;

                try
                {
                    TripReadDto tripCreated = await _tripService.CreateTripAsync(trip);
                    return tripCreated;
                }
                catch (ApiException ex)
                {
                    MessageBox.Show(
                        $"Error {ex.StatusCode}:\n{ex.ErrorDetails}",
                        "Error del servidor",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new InvalidOperationException($"Trip could not be created or retrieved: {raw.RideId}", ex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error inesperado: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    throw new InvalidOperationException($"Trip could not be created or retrieved: {raw.RideId}", ex);
                }

                //return trip;
            }
            else 
            {
                // Aqui va la logica para cancelar el viaje. Hay que verificar que sea cancelacion. o simplemente se intenta importar mas datos parciales de ese Funding source.
                return matchingTrip;
            }

            
        }

        private async Task<Coordinates> GetCoordinates(string street, string city, string state, string zip)
        {
            var address = $"{street}, {city}, {state}, {zip}";
            return await _googleMapsService.GetCoordinatesFromAddress(address);
        }

        private DateTime ParseDate(string value)
        {           
            return DateTime.TryParseExact(value, new[] { "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy", "M-d-yyyy", "MM-dd-yyyy", "yyyy/MM/dd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new FormatException($"Invalid date: {value}");
        }

        private TimeSpan ParseTime(string value)
        {
            return TimeSpan.TryParseExact(value, "h\\:mm\\:ss tt", CultureInfo.InvariantCulture, out var time)
                ? time
                : TimeSpan.TryParse(value, out time)
                    ? time
                    : throw new FormatException($"Invalid time: {value}");
        }

        private static CustomerCreateDto MapToCustomerCreateDto(Customer customer)
        {
            return new CustomerCreateDto
            {  
                RiderId = customer.RiderId,
                FullName = customer.FullName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Zip = customer.Zip,
                Phone = customer.Phone,
                MobilePhone = customer.MobilePhone,
                Email = customer.Email,
                FundingSourceId = customer.FundingSourceId,            
                SpaceTypeId = customer.SpaceTypeId,               
                Gender = customer.Gender,
                Created = customer.Created,
                CreatedBy = customer.CreatedBy
            };
        }
    }

}
