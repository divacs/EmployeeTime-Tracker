using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Added for configuration
using EmployeeTimeTracker.Models;

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
                        return View(groupedEmployees);
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
    }
}
