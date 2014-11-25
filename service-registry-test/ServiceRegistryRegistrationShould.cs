using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace service_registry_test
{
    [TestClass]
    public class ServiceRegistryRegistrationShould
    {
        ServiceRegistryRegistrationShould _ServiceRegistry;

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
            registrationTicket.TicketGoodUntil.Should().BeExactly(DateTime.MinValue);
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
            registrationTicket.TicketGoodUntil.Should().BeExactly(DateTime.MinValue);
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
            registrationTicket.TicketGoodUntil.Should().BeExactly(DateTime.MinValue);
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
            registrationTicket.TicketGoodUntil.Should().BeExactly(DateTime.MinValue);
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
            registrationTicket.TicketGoodUntil.Should().BeExactly(DateTime.MinValue);
        }
    }
}
