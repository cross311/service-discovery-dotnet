using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using service_registry_dotnet;
using System.Collections.Generic;

namespace service_registry_test
{
    [TestClass]
    public class ServiceRegistryRegistrationShould
    {
        ServiceRegistry _ServiceRegistry;

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
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeTrue();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.None);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().NotBe(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().BeWithin(TimeSpan.FromMinutes(2));
        }

        [TestMethod]
        public void RejectEmptyResource()
        {
            var resource = string.Empty;
            var instanceServiceUri = "http://testservice.com/api/v1";
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.ResourceMustNotBeEmpty);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectEmptyInstanceServiceUri()
        {
            var resource = "test-service";
            var instanceServiceUri = string.Empty;
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceServiceUriMustNotBeEmpty);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectNoneUriInstanceServiceUri()
        {
            var resource = "test-service";
            var instanceServiceUri = "badUri";
            var instanceHealthCheckUri = "http://testservice.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceServiceUriMustBeValidUri);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectEmptyInstanceHealthCheckUri()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var instanceHealthCheckUri = string.Empty;

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceHealthCheckUriMustNotBeEmpty);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void RejectNoneUriInstanceHealthCheckUri()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice.com/api/v1";
            var instanceHealthCheckUri = "badUri";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);

            var registrationTicket = _ServiceRegistry.Register(registration);

            registrationTicket.Success.Should().BeFalse();
            registrationTicket.FailReason.Should().Be(RegistrationFailReasons.InstanceHealthCheckUriMustBeValidUri);
            registrationTicket.InstanceRegistrationUniqueIdentifier.Should().Be(Guid.Empty);
            registrationTicket.Resource.Should().Be(resource);
            registrationTicket.InstanceServiceUri.Should().Be(instanceServiceUri);
            registrationTicket.TicketGoodUntil.Should().Be(DateTime.MinValue);
        }

        [TestMethod]
        public void AssignDifferentUniqueIdentifiersForDifferentServiceUris()
        {
            var resource = "test-service";
            var instanceServiceUri1 = "http://testservice1.com/api/v1";
            var instanceHealthCheckUri1 = "http://testservice1.com/health_check";
            var instanceServiceUri2 = "http://testservice2.com/api/v1";
            var instanceHealthCheckUri2 = "http://testservice2.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri1, instanceHealthCheckUri1);
            var ticket1 = _ServiceRegistry.Register(registration);

            registration = new ServiceRegistration(resource, instanceServiceUri2, instanceHealthCheckUri2);
            var ticket2 = _ServiceRegistry.Register(registration);

            ticket1.InstanceRegistrationUniqueIdentifier.Should().NotBe(ticket2.InstanceRegistrationUniqueIdentifier);
        }

        [TestMethod]
        public void AssignSameUniqueIdentifiersForSameServiceUris()
        {
            var resource = "test-service";
            var instanceServiceUri = "http://testservice1.com/api/v1";
            var instanceHealthCheckUri = "http://testservice1.com/health_check";

            var registration = new ServiceRegistration(resource, instanceServiceUri, instanceHealthCheckUri);
            var ticket1 = _ServiceRegistry.Register(registration);
            var ticket2 = _ServiceRegistry.Register(registration);

            ticket1.InstanceRegistrationUniqueIdentifier.Should().Be(ticket2.InstanceRegistrationUniqueIdentifier);
        }
    }
}
