﻿using Test.It.Specifications;
using Test.It.While.Hosting.Your.Service;
using Test.It.With.RabbitMQ091.Integration.Tests.TestApplication;

namespace Test.It.With.RabbitMQ091.Integration.Tests
{
    public class TestApplicationBuilder<TApplication> : DefaultServiceBuilder where TApplication : IApplication, new()
    {
        public override IServiceHost Create(ITestConfigurer configurer)
        {
            var testApplicationSpecification = new TApplication();
            testApplicationSpecification.Configure(resolver =>
            {
                resolver.AllowOverridingRegistrations();
                configurer.Configure(resolver);
                resolver.DisallowOverridingRegistrations();
            });
            return new ApplicationWrapper(testApplicationSpecification);
        }
    }
}