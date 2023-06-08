using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NetCoreWebDavApplication2
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.webdav.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            HostingEnvironment = env;
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            /* The relevant part for Forwarded Headers */
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddRazorPages();
            services.AddServerSideBlazor(options =>
            {
                //je vire ces 3 options la pour voir
#if DEBUG
                options.DetailedErrors = true;
#endif
                //options.DisconnectedCircuitMaxRetained = 200;
                //options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromHours(8);//
                options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
                //options.MaxBufferedUnacknowledgedRenderBatches = 10;
            })
                .AddHubOptions(o =>
                {
                    //déconseillé, mais pour la modal d'édition de,pdf syncfusion pour le moement il semblerait qu'on ait pas le choix
                    //todo poster un msg sur notre compte syncfusion (linkedin de didier)
                    o.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB;/
                    //je test d'enlever cette ligne voir ce que ca fait car pas du tout recommandé par microsoft
                    //en fait c'est encore pire en signalr, multiple deconexion/recox, si on enleve cette ligne, a voir eavec eux
                    //o.MaximumReceiveMessageSize = 100 * 1024 * 1024; // 100MB;//notamment pour l'upload de l'image usager via webcam
                    //o.ClientTimeoutInterval = TimeSpan.FromMinutes(30);//test pour la deconnexion mobile
                    //o.ClientTimeoutInterval = TimeSpan.FromMinutes(15);
                    //o.KeepAliveInterval = TimeSpan.FromMinutes(15);
                    //o.HandshakeTimeout = TimeSpan.FromMinutes(15);
                });
            services.AddHttpContextAccessor();
            services.AddScoped<HttpContextAccessor>();
            //pServices.AddSingleton(typeof(ISyncfusionStringLocalizer), typeof(SyncfusionServerLocalizer));
            services.Configure<RequestLocalizationOptions>(options =>
            {
                // Define the list of cultures your app will support
                var supportedCultures = new List<CultureInfo>()
                {
                    new CultureInfo("fr"),
                };
                // Set the default culture
                options.DefaultRequestCulture = new RequestCulture("fr");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddWebDav(Configuration, HostingEnvironment);

            //Enables web sockets. Web sockets are used to update the documents list in case of any changes on the server.
            services.AddSingleton<WebSocketsService>();
      
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRequestLocalization("fr");

            /* The relevant part for Forwarded Headers */
            app.UseForwardedHeaders();

            app.UseResponseCompression();

            //app.UseBlazorFrameworkFiles();

            app.UseStaticFiles();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseCookiePolicy();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapBlazorHub(configureOptions: options =>
                {
                    //options.Transports = HttpTransportType.LongPolling;//pour test
                });
                endpoints.MapFallbackToPage("/_Host");
            });

            //Enables web sockets. Web sockets are used to update the documents list in case of any changes on the server.
            app.UseWebSockets();
            app.UseWebSocketsMiddleware();
            app.UseWebDav(HostingEnvironment);
        }
    }
}
