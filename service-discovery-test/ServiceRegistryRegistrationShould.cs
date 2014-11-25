using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using service_discovery;
using System.Collections.Generic;

namespace service_registry_test
{
    [TestClass]
    public class ServiceRegistryRegistrationShould
    {
        IServiceRegistry _ServiceRegistry;

        [TestInitialize]
        public void TestInitialize()
        {
            _ServiceRegistry = new ServiceRegistry();
        }

        [TestMethod]
        public void AcceptValidRegistrations()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeTrue();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.None);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [TestMethod]
        public void RejectEmptyResource()
        {
            var resource = string.Empty;
            var instanceServiceUri = "http://testservice.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.ResourceMustNotBeEmpty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectEmptyInstanceServiceUri()
        {
            var resource = "test-service";
            var instanceServiceUri = string.Empty;

            var registration = new ServiceRegistration(resource, instanceServiceUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceServiceUriMustNotBeEmpty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectNoneUriInstanceServiceUri()
        {
            var resource = "test-service";
            var instanceServiceUri = "badUri";

            var registration = new ServiceRegistration(resource, instanceServiceUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceServiceUriMustBeValidUri);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectZeroTimeToLive()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var timeToLive = TimeSpan.Zero;

            var registration = new ServiceRegistration(resource, instanceServiceUri, timeToLive);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.TimeToLiveMustBeGreaterThanZero);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void AssignDifferentUniqueIdentifiersForDifferentServiceUris()
        {
            var resource = "test-service";
            var instanceServiceUri1 = "http://testservice1.com/api/v1";
            var instanceServiceUri2 = "http://testservice2.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri1);
            var ticket1 = _ServiceRegistry.Register(registration);

            registration = new ServiceRegistration(resource, instanceServiceUri2);
            var ticket2 = _ServiceRegistry.Register(registration);

            ticket1.InstanceServiceUri.Should().NotBe(ticket2.InstanceServiceUri);
        }

        [TestMethod]
        public void AssignSameUniqueIdentifiersForSameServiceUris()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice1.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri);
            var ticket1 = _ServiceRegistry.Register(registration);
            var ticket2 = _ServiceRegistry.Register(registration);

            ticket1.InstanceServiceUri.Should().Be(ticket2.InstanceServiceUri);
        }

        [TestMethod]
        public void DefaultRegistrationsTimeToLiveToInfinite()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.RegistrationExpiresAt.Should().Be(DateTime.MaxValue);
        }

        [TestMethod]
        public void RespectRegistrationsTimeToLive()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var timeToLive = TimeSpan.FromMinutes(30);

            var registration = new ServiceRegistration(resource, instanceServiceUri, timeToLive);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.RegistrationExpiresAt.Should().BeCloseTo(DateTime.UtcNow.Add(timeToLive));
        }
    }
}
