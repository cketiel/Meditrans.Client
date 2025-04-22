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
    internal class GoogleMapsService
    {
        private static readonly string apiKey = App.Configuration["GoogleMaps:ApiKey"];
        private static readonly HttpClient client = new HttpClient();

        // traffic_model: Se puede establecer en best_guess (la mejor estimación), pessimistic (el peor escenario), o optimistic (el mejor escenario).
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

    }
    public class RouteDetail
    {
        public string Distance { get; set; } = "";
        public string Duration { get; set; } = "";
        public string DurationInTraffic { get; set; } = "";
    }

}
