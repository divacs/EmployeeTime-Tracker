using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Added for configuration
using EmployeeTimeTracker.Models;
using System.Drawing.Imaging;
using System.Drawing;

namespace EmployeeTimeTracker.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration; // Added for configuration

        public EmployeeController(IHttpClientFactory clientFactory, IConfiguration configuration) 
        {
            _clientFactory = clientFactory;
            _configuration = configuration; // Dependency injection of IHttpClientFactory and IConfiguration
        }

        // Action method to retrieve employee data  
        public async Task<IActionResult> Employee()
        {
            // Getting API URL and API key from appsettings.json
            string apiUrl = _configuration["ApiUrl"];
            string apiKey = _configuration["ApiKey"];
            string url = $"{apiUrl}?code={apiKey}";

            try
            {
                var client = _clientFactory.CreateClient(); // Creating HttpClient instance

                // Sending HTTP GET request to the API endpoint
                using (var response = await client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        // Deserializing the JSON response into a list of EmployeeModel objects
                        var responseStream = await response.Content.ReadAsStreamAsync();
                        var employees = await JsonSerializer.DeserializeAsync<List<EmployeeModel>>(responseStream);

                        // Grouping employees by their ID and calculating total worked hours for each employee
                        var groupedEmployees = employees.GroupBy(e => e.Id.ToString())
                            .Select(group => new EmployeeModel
                            {
                                Id = Guid.Parse(group.Key),
                                EmployeeName = group.First().EmployeeName,
                                TotalWorkedHours = group.Sum(e => (e.EndTimeUtc - e.StarTimeUtc).TotalHours)
                            })
                            .OrderByDescending(e => e.TotalWorkedHours)
                            .ToList();

                        // Returning the grouped and sorted employee data to the view
                        // Generate pie chart
                        var image = GeneratePieChart(groupedEmployees);

                        // Return the image as a response
                        return File(image, "image/png");
                    }
                    else
                    {
                        // Returning a BadRequest result if the API request is unsuccessful
                        return BadRequest("Failed to retrieve data. Status code: " + response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                // Returning a BadRequest result with the exception message if an error occurs
                return BadRequest(ex.Message);
            }
        }

        // Method to generate pie chart
        private byte[] GeneratePieChart(List<EmployeeModel> employees)
        {
            // Create a new bitmap
            var bitmap = new Bitmap(400, 400);

            // Create graphics object from bitmap
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // Clear the graphics surface
                graphics.Clear(Color.White);

                // Define colors for pie slices
                Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Orange, Color.Purple, Color.Gray, Color.Pink, Color.Brown };

                // Calculate total worked hours
                double totalHours = employees.Sum(e => e.TotalWorkedHours);

                // Initialize start angle for pie chart slices
                float startAngle = 0;

                // Iterate over employees to draw pie chart slices
                for (int i = 0; i < employees.Count; i++)
                {
                    // Calculate angle for current slice based on its proportion of total hours
                    float sweepAngle = (float)(360 * employees[i].TotalWorkedHours / totalHours);

                    // Define brush for current slice
                    using (var brush = new SolidBrush(colors[i % colors.Length]))
                    {
                        // Draw pie chart slice
                        graphics.FillPie(brush, new Rectangle(0, 0, 400, 400), startAngle, sweepAngle);
                    }

                    // Update start angle for next slice
                    startAngle += sweepAngle;
                }
            }

            // Convert bitmap to byte array (PNG format)
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

    }
}
