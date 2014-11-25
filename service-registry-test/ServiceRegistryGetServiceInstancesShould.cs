using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using service_registry_dotnet;
using FluentAssertions;
using System.Linq;

namespace service_registry_test
{
    [TestClass]
    public class ServiceRegistryGetServiceInstancesShould
    {
        IServiceRegistry _ServiceRegistry;

        [TestInitialize]
        public void TestInitialize()
        {
            _ServiceRegistry = new ServiceRegistry();
        }

        [TestMethod]
        public void ReturnZeroInstancesIfNoneRegistered()
        {
            var resource = "test-service";
            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(0);
        }

        [TestMethod]
        public void ReturnOneInstanceIfOneRegistered()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(1);
            var instance = serviceInstances[0];
            instance.ServiceUri.Should().Be(instanceServiceUri);
            instance.RegistrationExpiresAt.Should().BeWithin(TimeSpan.FromSeconds(1));
            instance.UniqueIdentifier.Should().NotBe(Guid.Empty);
        }

        [TestMethod]
        public void ReturnTwoInstancesIfTwoDifferentSericeUrisRegistered()
        {
            var resource = "test-service";
            var instanceServiceUri1 = "http://testservice1.com/api/v1";
            var instanceHealthCheckUri1 = "http://testservice1.com/health_check";
            var instanceServiceUri2 = "http://testservice2.com/api/v1";
            var instanceHealthCheckUri2 = "http://testservice2.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri1, instanceHealthCheckUri1);
            _ServiceRegistry.Register(registration);

            registration = new ServiceRegistration(resource, instanceServiceUri2, instanceHealthCheckUri2);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(2);
            serviceInstances.Should().Contain((instance) => instance.ServiceUri == instanceServiceUri1);
            serviceInstances.Should().Contain((instance) => instance.ServiceUri == instanceServiceUri2);
        }

        [TestMethod]
        public void ReturnOneInstancesIfSericeUriRegisteredTwice()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice1.com/api/v1";
            var instanceHealthCheckUri = "http://testservice1.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);
            _ServiceRegistry.Register(registration);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(1);
            serviceInstances.Should().Contain((instance) => instance.ServiceUri == instanceServiceUri);
        }

        [TestMethod]
        public void NotReturnInstancesThatHaveNotCheckedInWithinConfiguredTime()
        {
            _ServiceRegistry = new ServiceRegistry(TimeSpan.FromSeconds(-10));

            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(0);
        }
    }
}
