using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using FloKaptureJobProcessingApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.EntityFrameworkCore.Internal;

namespace FloKaptureJobRestfulSelfHostApp
{
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine($@"=======================================================================");
            var host = new WebHostBuilder().UseKestrel(options => options.ConfigureEndpoints()).UseStartup<StartUp>().Build();
            host.Run();
        }
    }
    public static class KestrelServerOptionsExtensions
    {
        public static void ConfigureEndpoints(this KestrelServerOptions options)
        {
            var environment = options.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            if (AppDomain.CurrentDomain.BaseDirectory == null) return;
            string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new[] { @"bin\" }, StringSplitOptions.None).First();
            var configurationRoot = new ConfigurationBuilder().SetBasePath(projectPath).AddJsonFile("appsettings.json").Build();
            var endpoints = configurationRoot.GetSection("HttpServer:EndPoints").GetChildren()
                .ToDictionary(section => section.Key, section =>
                {
                    var endpoint = new EndPointConfiguration();
                    section.Bind(endpoint);
                    return endpoint;
                });
            Console.WriteLine($@"=== Adding configurations for protocols and ports to use. ===");

            foreach (var endpoint in endpoints)
            {
                var config = endpoint.Value;
                var port = config.Port ?? (config.Scheme == "https" ? 443 : 80);

                var ipAddresses = new List<IPAddress>();
                if (config.Host == "localhost")
                {
                    ipAddresses.Add(IPAddress.IPv6Loopback);
                    ipAddresses.Add(IPAddress.Loopback);
                }
                else if (IPAddress.TryParse(config.Host, out var address))
                {
                    ipAddresses.Add(address);
                }
                else
                {
                    ipAddresses.Add(IPAddress.IPv6Any);
                }

                foreach (var address in ipAddresses)
                {
                    options.Listen(address, port, listenOptions =>
                    {
                        if (config.Scheme != "https") return;
                        var certificate = LoadCertificate(config);
                        listenOptions.UseHttps(certificate);
                    });
                }
            }
        }

        private static X509Certificate2 LoadCertificate(EndPointConfiguration config)
        {
            if (!string.IsNullOrEmpty(config.StoreName) && !string.IsNullOrEmpty(config.StoreLocation))
            {
                using var x509Store = new X509Store(config.StoreName, Enum.Parse<StoreLocation>(config.StoreLocation));
                x509Store.Open(OpenFlags.ReadOnly);
                var certificates = x509Store.Certificates.Find(X509FindType.FindBySubjectName, config.Host, false);

                /*
                    if (!certificates.Any())
                    {
                        throw new InvalidOperationException($"Certificate not found for: {config.Host}.");
                    }
                    */

                return certificates.OfType<X509Certificate2>().First();
            }

            if (!string.IsNullOrEmpty(config.FilePath) && !string.IsNullOrEmpty(config.Password))
            {
                return new X509Certificate2(config.FilePath, config.Password);
            }

            throw new InvalidOperationException("No valid certificate configuration found for the current endpoint.");
        }
    }

    public class EndPointConfiguration
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Scheme { get; set; }
        public string StoreName { get; set; }
        public string StoreLocation { get; set; }
        public string FilePath { get; set; }
        public string Password { get; set; }
    }
}
