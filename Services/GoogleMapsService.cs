using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Meditrans.Client.Models;
using Newtonsoft.Json.Linq;

namespace Meditrans.Client.Services
{
    public class GoogleMapsService
    {
        private static readonly string apiKey = App.Configuration["GoogleMaps:ApiKey"];
        private static readonly HttpClient client = new HttpClient();

        public async Task<RouteFullDetail> GetRouteFullDetails(double originLat, double originLng, double destLat, double destLng)
        {
            RouteFullDetail? routeFullDetail = null;

            string url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                         $"origin={originLat},{originLng}&destination={destLat},{destLng}&" +
                         $"departure_time=now&traffic_model=best_guess&key={apiKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseBody);

            var route = jsonResponse["routes"]?.First;
            var leg = route?["legs"]?.First;

            var distance = leg?["distance"]?["text"]?.ToString();
            var duration = leg?["duration"]?["text"]?.ToString();
            var durationInTraffic = leg?["duration_in_traffic"]?["text"]?.ToString();           

            var distanceMiles = leg?["distance"]?["text"]?.ToString();
            var durationMinutes = leg?["duration"]?["text"]?.ToString();
            var durationInTrafficMinutes = leg?["duration_in_traffic"]?["text"]?.ToString();

            var distanceMeters = leg?["distance"]?["value"]?.ToString();
            var durationSeconds = leg?["duration"]?["value"]?.ToString();
            var durationInTrafficSeconds = leg?["duration_in_traffic"]?["value"]?.ToString();

            routeFullDetail.DistanceString = distance;
            routeFullDetail.DurationString = duration;
            routeFullDetail.DurationInTrafficString = durationInTraffic;

            routeFullDetail.DistanceMiles = double.Parse(distanceMiles);
            routeFullDetail.DurationMinutes = double.Parse(durationMinutes);
            routeFullDetail.DurationInTrafficMinutes = double.Parse(durationInTrafficMinutes);

            routeFullDetail.DistanceMeters = double.Parse(distanceMeters);
            routeFullDetail.DurationSeconds = double.Parse(durationSeconds);
            routeFullDetail.DurationInTrafficSeconds = double.Parse(durationInTrafficSeconds);


            return routeFullDetail;

        }

        // traffic_model: Can be set to best_guess (best guess), pessimistic (worst case scenario), or optimistic (best case scenario).
        public async Task<RouteDetail> GetRouteDetails(double originLat, double originLng, double destLat, double destLng)
        {
            RouteDetail? routeDetail = null;

            string url = $"https://maps.googleapis.com/maps/api/directions/json?" +
                         $"origin={originLat},{originLng}&destination={destLat},{destLng}&" +
                         $"departure_time=now&traffic_model=best_guess&key={apiKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jsonResponse = JObject.Parse(responseBody);

            var route = jsonResponse["routes"]?.First;
            var leg = route?["legs"]?.First;

            var distance = leg?["distance"]?["text"]?.ToString();
            var duration = leg?["duration"]?["text"]?.ToString();
            var durationInTraffic = leg?["duration_in_traffic"]?["text"]?.ToString();

            Console.WriteLine($"Distance: {distance}");
            Console.WriteLine($"Duration (without traffic): {duration}");
            Console.WriteLine($"Duration (with traffic): {durationInTraffic}");

            routeDetail.Distance = distance;
            routeDetail.Duration = duration;
            routeDetail.DurationInTraffic = durationInTraffic;

            return routeDetail;

        }

        // Method to obtain four durations and the distance
        public async Task<(string noTraffic, string withTraffic, string pessimistic, string optimistic, string distance)>
            GetRouteDurationsAndDistance(double originLat, double originLng, double destLat, double destLng)
        {
            string urlTemplate = "https://maps.googleapis.com/maps/api/directions/json?" +
                                 "origin={0},{1}&destination={2},{3}&departure_time=now&key={4}&traffic_model={5}";

            // We'll use the same duration and distance for both "no traffic" and "with traffic" using "best_guess" (this is a simplification):
            var infoBestGuess = await GetRouteInfo(urlTemplate, "best_guess", originLat, originLng, destLat, destLng);
            var infoPessimistic = await GetRouteInfo(urlTemplate, "pessimistic", originLat, originLng, destLat, destLng);
            var infoOptimistic = await GetRouteInfo(urlTemplate, "optimistic", originLat, originLng, destLat, destLng);

            // Here, for simplicity, we use the result from best_guess for both "noTraffic" and "withTraffic"
            return (infoBestGuess.duration, infoBestGuess.duration, infoPessimistic.duration, infoOptimistic.duration, infoBestGuess.distance);
        }

        // Auxiliary method that returns a tuple with duration and distance for a given traffic model
        private async Task<(string duration, string distance)> GetRouteInfo(string urlTemplate, string trafficModel, double originLat, double originLng, double destLat, double destLng)
        {
            string url = string.Format(urlTemplate, originLat, originLng, destLat, destLng, apiKey, trafficModel);

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseBody);

                var route = jsonResponse["routes"]?.First;
                var leg = route?["legs"]?.First;

                // Get the distance
                string distance = leg?["distance"]?["text"]?.ToString() ?? "N/A";

                // Get the duration
                // We use duration_in_traffic if available; otherwise, fall back to duration.
                string duration = leg?["duration_in_traffic"]?["text"]?.ToString();
                if (string.IsNullOrEmpty(duration))
                {
                    duration = leg?["duration"]?["text"]?.ToString() ?? "N/A";
                }

                return (duration, distance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving route info: {ex.Message}");
                return ("Error", "Error");
            }
        }

        public async Task<Coordinates?> GetCoordinatesFromAddress(string address)
        {

            string apiKey = App.Configuration["GoogleMaps:ApiKey"];
            var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={apiKey}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            if (json["status"]?.ToString() == "OK")
            {
                var location = json["results"]?[0]?["geometry"]?["location"];
                if (location != null)
                {
                    return new Coordinates
                    {
                        Latitude = (double)location["lat"],
                        Longitude = (double)location["lng"]
                    };
                }
            }

            return null;
        }
        public async Task<Coordinates> GetCoordinates(string street, string city, string state, string zip)
        {
            var address = $"{street}, {city}, {state}, {zip}";
            return await GetCoordinatesFromAddress(address);
        }

    }

    public class RouteFullDetail
    {
        public string DistanceString { get; set; } = "";
        public string DurationString { get; set; } = "";
        public string DurationInTrafficString { get; set; } = "";
        public double DistanceMiles { get; set; }
        public double DurationMinutes { get; set; }
        public double DurationInTrafficMinutes { get; set; }
        public double DistanceMeters { get; set; }
        public double DurationSeconds { get; set; }
        public double DurationInTrafficSeconds { get; set; }

    }
    public class RouteDetail
    {
        public string Distance { get; set; } = "";
        public string Duration { get; set; } = "";
        public string DurationInTraffic { get; set; } = "";
    }
    public class Coordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
