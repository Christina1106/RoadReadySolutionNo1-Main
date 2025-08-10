using System;
using System.Threading.Tasks;
using RoadReady1.Interfaces;

namespace RoadReady1.Services
{
    /// <summary>
    /// Simple IEmailService that writes email contents to the console.
    /// </summary>
    public class ConsoleEmailService : IEmailService
    {
        public Task SendAsync(string to, string subject, string htmlContent)
        {
            Console.WriteLine($"\n=== Sending Email ===\nTo: {to}\nSubject: {subject}\nBody:\n{htmlContent}\n=====================\n");
            return Task.CompletedTask;
        }
    }
}
