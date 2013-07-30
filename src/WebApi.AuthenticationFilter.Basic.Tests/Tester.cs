﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace WebApi.AuthenticationFilter.Basic.Tests
{
    public static class Tester
    {
        public async static Task Run(
            Action<HttpConfiguration> withConfiguration,
            Func<HttpRequestMessage> withRequest,
            Func<ApiController, HttpResponseMessage> assertInAction,
            Action<HttpResponseMessage> assertResponse)
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{id}", new { controller = "test", action = "get", id = RouteParameter.Optional });
            var service = new TestControllerService(config, assertInAction);
            config.Services.Replace(typeof(IHttpControllerActivator), service);
            withConfiguration(config);

            var server = new HttpServer(config);
            var client = new HttpClient(server);
            var resp = await client.SendAsync(withRequest());
            assertResponse(resp);
        }
    }

    public class TestController : ApiController
    {
        private readonly Func<ApiController, HttpResponseMessage> _assert;

        public TestController(Func<ApiController, HttpResponseMessage> assert)
        {
            Configuration = new HttpConfiguration();
            _assert = assert;
        }

        public HttpResponseMessage Get()
        {
            return _assert(this);
        }
    }

    public class TestControllerService : IHttpControllerActivator
    {
        private readonly HttpConfiguration _config;
        private readonly Func<ApiController, HttpResponseMessage> _action;

        public TestControllerService(HttpConfiguration config, Func<ApiController, HttpResponseMessage> action)
        {
            _config = config;
            _action = action;
        }

        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            if (controllerType != typeof(TestController)) throw new ArgumentException();
            return new TestController(_action);
        }
    }
}