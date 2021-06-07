using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace FloKaptureJobProcessingApp
{
    public class StartUp
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                })
                .AddMvc(d => { d.EnableEndpointRouting = false; })
                .AddControllersAsServices()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddControllers().AddNewtonsoftJson(jsonOptions =>
            {
                if (AppDomain.CurrentDomain.BaseDirectory == null) return;
                string projectPath = AppDomain.CurrentDomain.BaseDirectory.Split(new[] { @"bin\" }, StringSplitOptions.None).First();
                var configurationRoot = new ConfigurationBuilder().SetBasePath(projectPath).AddJsonFile("appsettings.json").Build();
                string dateFormat = configurationRoot.GetSection("DateFormat").Value;
                jsonOptions.SerializerSettings.Converters.Add(new IsoDateTimeConverter
                {
                    DateTimeFormat = dateFormat,
                    Culture = new CultureInfo("en-US")
                    {
                        DateTimeFormat = new DateTimeFormatInfo
                        {
                            ShortDatePattern = dateFormat
                        }
                    },
                    DateTimeStyles = DateTimeStyles.AdjustToUniversal
                });
                jsonOptions.SerializerSettings.DateFormatString = dateFormat;
                jsonOptions.SerializerSettings.Formatting = Formatting.Indented;
                jsonOptions.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                jsonOptions.AllowInputFormatterExceptionMessages = true;
                jsonOptions.SerializerSettings.SerializationBinder = new DefaultSerializationBinder();
                jsonOptions.SerializerSettings.ContractResolver = new DictionaryAsArrayResolver();
            });
        }
        public void Configure(IApplicationBuilder app)
        {
            app.UseCors();
            app.UseMvcWithDefaultRoute();
            app.UseHttpsRedirection();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "api/{controller}/{action}/{id}", defaults: new { id = RouteParameter.Optional, action = RouteParameter.Optional });
                routes.MapRoute(name: "job-api", template: "job/{controller}/{action}/{id}", defaults: new { id = RouteParameter.Optional, action = RouteParameter.Optional });
                routes.MapRoute(name: "config-api", template: "config/{controller}/{action}/{id}", defaults: new { id = RouteParameter.Optional, action = RouteParameter.Optional });
            });
        }
    }

    public class AutomaticModelStateValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            if (!actionExecutingContext.ModelState.IsValid)
            {
                actionExecutingContext.Result = new BadRequestObjectResult(actionExecutingContext.ModelState);
            }
        }
    }
    public sealed class RouteParameter
    {
        public static readonly RouteParameter Optional = new RouteParameter();
        private RouteParameter()
        {
        }
        public override string ToString()
        {
            return string.Empty;
        }
    }

    [DebuggerStepThrough]
    internal class DictionaryAsArrayResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            return objectType.GetInterfaces().Any(i => i == typeof(IDictionary) || i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ? CreateArrayContract(objectType) : base.CreateContract(objectType);
        }
        [DebuggerStepThrough]
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return type.GetProperties().Select(p =>
            {
                var jp = CreateProperty(p, memberSerialization);
                jp.ValueProvider = new NullToEmptyStringValueProvider(p);
                return jp;
            }).ToList();
        }
    }

    [DebuggerStepThrough]
    public class NullToEmptyStringValueProvider : IValueProvider
    {
        private readonly PropertyInfo _memberInfo;
        public NullToEmptyStringValueProvider(PropertyInfo memberInfo)
        {
            _memberInfo = memberInfo;
        }

        [DebuggerStepThrough]
        public object GetValue(object target)
        {
            var result = _memberInfo.GetValue(target);
            if (_memberInfo.PropertyType == typeof(string) && result == null) result = "";
            return result;
        }
        [DebuggerStepThrough]
        public void SetValue(object target, object value)
        {
            _memberInfo.SetValue(target, value);
        }
    }

    [DebuggerStepThrough]
    public class BrowserJsonFormatter : JsonMediaTypeFormatter
    {
        public BrowserJsonFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/jsonp"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/text"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml"));
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/plain"));
            SerializerSettings.Formatting = Formatting.Indented;
        }
        public override void SetDefaultContentHeaders(Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
        {
            base.SetDefaultContentHeaders(type, headers, mediaType);
            headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
    }
}
