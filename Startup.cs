using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Stripe;

namespace Octagon.Formatik.Payment
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<StripeSettings>(Configuration.GetSection("Stripe"));
            services.Configure<FormatikSettings>(Configuration.GetSection("Formatik"));

            services.AddMvc();
        }

        private async Task GlobalErrorHandler(HttpContext context)
        {
            IExceptionHandlerFeature ex = null;
            var errorReference = ObjectId.GenerateNewId();

            try
            {
                ex = context.Features.Get<IExceptionHandlerFeature>();

                var logDb = Common.GetDB(Configuration.GetValue<string>("Formatik:LogsDbConnection"));
                using (var body = new StreamReader(context.Request.Body))
                {
                    await logDb
                        .GetCollection<PaymentException>("Exceptions")
                        .InsertOneAsync(new PaymentException()
                        {
                            _id = errorReference,
                            Method = context.Request.Method,
                            Request = $"{(context.Request.IsHttps ? "https" : "http")}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}",
                            Body = context.Request.Method == "POST" ?
                                    body.ReadToEnd() :
                                    null,
                            Headers = String.Join("\r\n", context.Request.Headers.Select(pair => $"{pair.Key} = {pair.Value}")),
                            UserAddress = context.Connection.RemoteIpAddress.ToString(),
                            Exception = ex.Error.Message,
                            StackTrace = ex.Error.StackTrace,
                            Timestamp = DateTime.Now,
                            Process = System.Environment.MachineName
                        })
                        .ConfigureAwait(false);
                }
            }
            catch (Exception loggingException)
            {
                await Console.Error.WriteLineAsync(loggingException.Message).ConfigureAwait(false);
                await Console.Error.WriteLineAsync(loggingException.StackTrace).ConfigureAwait(false);
            }

            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";

                if (ex != null)
                {
                    await context.Response
                        .WriteAsync($"{{\"status\":\"ERROR\",\"error\":\"Internal Server Error\",\"errorReferense\":\"{errorReference}\"}}")
                        .ConfigureAwait(false);
                }
            }
            catch (Exception loggingException)
            {
                await Console.Error.WriteLineAsync(loggingException.Message).ConfigureAwait(false);
                await Console.Error.WriteLineAsync(loggingException.StackTrace).ConfigureAwait(false);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            StripeConfiguration.SetApiKey(Configuration.GetSection("Stripe")["SecretKey"]);

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // global error handler
            app.UseExceptionHandler(options =>
            {
                options.Run(GlobalErrorHandler);
            });

            app.UseResponseCompression();
            app.UseMvc();
        }
    }
}
