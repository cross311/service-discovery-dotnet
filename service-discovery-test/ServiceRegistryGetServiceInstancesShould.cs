using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using service_discovery;
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
            var instanceTags = new[] {"tag:test"};

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceTags);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(1);
            var instance = serviceInstances[0];
            instance.ServiceUri.Should().Be(instanceServiceUri);
            instance.RegistrationExpiresAt.Should().BeWithin(TimeSpan.FromSeconds(1));
            instance.Tags.Should().Contain(instanceTags[0]);
        }

        [TestMethod]
        public void ReturnTwoInstancesIfTwoDifferentSericeUrisRegistered()
        {
            var resource = "test-service";
            var instanceServiceUri1 = "http://testservice1.com/api/v1";
            var instanceServiceUri2 = "http://testservice2.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri1);
            _ServiceRegistry.Register(registration);

            registration = new ServiceRegistration(resource, instanceServiceUri2);
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

            var registration = new ServiceRegistration(resource, instanceServiceUri);
            _ServiceRegistry.Register(registration);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(1);
            serviceInstances.Should().Contain((instance) => instance.ServiceUri == instanceServiceUri);
        }

        [TestMethod]
        public void NotReturnInstancesThatHaveNotCheckedInWithinRegisteredTimeToLive()
        {
            var timeToLive = TimeSpan.FromTicks(1);
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri, timeToLive);
            _ServiceRegistry.Register(registration);

            var serviceInstances = _ServiceRegistry.GetServiceInstancesForResource(resource).ToList();

            serviceInstances.Count.Should().Be(0);
        }
    }
}
