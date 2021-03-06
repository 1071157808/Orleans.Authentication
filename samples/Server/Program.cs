﻿using IGrains;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Orleans.Authentication.IdentityServer4;
using Orleans.Authentication;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                RunMainAsync();
            }
            catch (Exception er)
            {
                Console.Write(er.Message);
            }
            Console.ReadKey();
        }

        private static async void RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var siloPort = 11111;
            int gatewayPort = 30000;
            var siloAddress = IPAddress.Loopback;

            var builder = new SiloHostBuilder()
                .UseEnvironment("Development")
                .UseDevelopmentClustering(options => options.PrimarySiloEndpoint = new IPEndPoint(siloAddress, siloPort))
                .ConfigureAppConfiguration(Startup.ConfigureAppConfiguration)
                .ConfigureServices(Startup.ConfigureServices)
                .ConfigureEndpoints(siloAddress, siloPort, gatewayPort)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(AuthorizeGrain).Assembly).WithReferences())
                .ConfigureLogging((HostBuilderContext context, ILoggingBuilder logging) =>
               {
                   logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                   logging.AddConsole();
               })
                .AddAuthentication((HostBuilderContext context, AuthenticationBuilder authen) =>
                {
                    authen.AddIdentityServerAuthentication(opt =>
                    {
                        var config = context.Configuration.GetSection("ApiAuth").Get<IdentityServerAuthenticationOptions>();
                        opt.RequireHttpsMetadata = config.Authority.Contains("https/");
                        opt.Authority = config.Authority;
                        opt.ApiName = config.ApiName;
                        opt.ApiSecret = config.ApiSecret;
                    });
                }, IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddAuthorizationFilter();

            //使用流
            //.AddKafkaStreams("Kafka", opt =>
            //{
            //    opt.TopicName = "Zop.Payment";
            //    opt.KafkaConfig.Add("group.id", "Orleans.Streams.Kafka.Group");
            //    opt.KafkaConfig.Add("socket.blocking.max.ms", 10);
            //    opt.KafkaConfig.Add("enable.auto.commit", false);
            //    opt.KafkaConfig.Add("bootstrap.servers", "120.79.162.19:9092");

            //});

            var host = builder.Build();
            await host.StartAsync();
            return host;


        }
    }
}
