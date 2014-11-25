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
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().NotBe(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.RegistrationExpiresAt.Should().BeWithin(TimeSpan.FromMinutes(2));
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
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
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
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
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
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
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

            ticket1.InstanceRegistrationUniqueIdentifier.Should().NotBe(ticket2.InstanceRegistrationUniqueIdentifier);
        }

        [TestMethod]
        public void AssignSameUniqueIdentifiersForSameServiceUris()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice1.com/api/v1";

            var registration = new ServiceRegistration(resource, instanceServiceUri);
            var ticket1 = _ServiceRegistry.Register(registration);
            var ticket2 = _ServiceRegistry.Register(registration);

            ticket1.InstanceRegistrationUniqueIdentifier.Should().Be(ticket2.InstanceRegistrationUniqueIdentifier);
        }
    }
}
