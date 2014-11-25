using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_discovery
{
    public class ServiceRegistry : IServiceRegistry
    {
        private static readonly IEnumerable<ServiceInstance> _EmptyServiceInstances = Enumerable.Empty<ServiceInstance>();
        private readonly IServiceRegistryRepository _ServiceRegistryRepository;

        public ServiceRegistry() 
            : this(new InMemoryServiceRegistryRepository()) { }

        public ServiceRegistry(TimeSpan checkInWithinTime)
            : this(new InMemoryServiceRegistryRepository(checkInWithinTime)) { }

        public ServiceRegistry(IServiceRegistryRepository serviceRegistryRepository)
        {
            if(ReferenceEquals(serviceRegistryRepository, null)) throw new ArgumentNullException("serviceRegistryRepository");

            _ServiceRegistryRepository = serviceRegistryRepository;
        }

        public RegistrationTicket Register(ServiceRegistration registration)
        {
            var resource = registration.Resource;
            var serviceUriString = registration.InstanceServiceUri;

            var invalidResponse =  ValidateRegistration(resource, serviceUriString);

            if (!ReferenceEquals(invalidResponse, null))
            {
                return invalidResponse;
            }

            var serviceInstance = _ServiceRegistryRepository.AddOrUpdate(resource, serviceUriString);

            var successResult = new RegistrationTicket(serviceInstance);

            return successResult;
        }

        public IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource)
        {
            if(string.IsNullOrWhiteSpace(resource))
                return _EmptyServiceInstances;

            var validInstances = _ServiceRegistryRepository.GetServiceInstancesForResource(resource);

            return validInstances;
        }

        private static RegistrationTicket ValidateRegistration(string resource, string serviceUriString)
        {
            if (string.IsNullOrWhiteSpace(resource))
            {
                var resourceNullResponse = new RegistrationTicket(RegistrationFailReasons.ResourceMustNotBeEmpty, resource, serviceUriString);
                return resourceNullResponse;
            }

            if (string.IsNullOrWhiteSpace(serviceUriString))
            {
                var serviceUriNullResponse = new RegistrationTicket(RegistrationFailReasons.InstanceServiceUriMustNotBeEmpty, resource, serviceUriString);
                return serviceUriNullResponse;
            }

            if (!Uri.IsWellFormedUriString(serviceUriString, UriKind.Absolute))
            {
                var serviceUriNotValid = new RegistrationTicket(RegistrationFailReasons.InstanceServiceUriMustBeValidUri, resource, serviceUriString);
                return serviceUriNotValid;
            }

            return null;
        }
    }

    public class ServiceRegistration
    {
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;

        public ServiceRegistration(string resource, string instanceServiceUri)
        {
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
        }

        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
    }

    public class RegistrationTicket
    {
        private readonly bool _Success;
        private readonly RegistrationFailReasons _FailReason;
        private readonly Guid _InstanceRegistrationUniqueIdentifier;
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly DateTime _RegistrationExpiresAt;

        internal RegistrationTicket(
            ServiceInstance serviceInstance)
            :this(
            true,
            RegistrationFailReasons.None,
            serviceInstance.UniqueIdentifier,
            serviceInstance.Resource,
            serviceInstance.ServiceUri,
            serviceInstance.RegistrationExpiresAt)
        {
        }

        internal RegistrationTicket(
            RegistrationFailReasons failReason,
            string resource,
            string instanceServiceUri)
            : this(
                false,
                failReason,
                Guid.Empty,
                resource,
                instanceServiceUri,
                DateTime.MinValue)
        {
        }


        private RegistrationTicket(
            bool success,
            RegistrationFailReasons failReason, 
            Guid instanceRegistrationUniqueIdentifier, 
            string resource,
            string instanceServiceUri, 
            DateTime registrationExpiresAt)
        {
            _Success = success;
            _FailReason = failReason;
            _InstanceRegistrationUniqueIdentifier = instanceRegistrationUniqueIdentifier;
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _RegistrationExpiresAt = registrationExpiresAt;
        }

        public bool Success { get { return _Success; } }
        public RegistrationFailReasons FailReason { get { return _FailReason; } }
        public Guid InstanceRegistrationUniqueIdentifier { get { return _InstanceRegistrationUniqueIdentifier; } }
        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public DateTime RegistrationExpiresAt { get { return _RegistrationExpiresAt; } }
    }

    public enum RegistrationFailReasons
    {
        None,
        ResourceMustNotBeEmpty,
        InstanceServiceUriMustNotBeEmpty,
        InstanceServiceUriMustBeValidUri
    }

    public class ServiceInstance
    {
        private readonly string _Resource;
        private readonly string _ServiceUri;
        private readonly DateTime _RegistrationExpiresAt;
        private readonly Guid _UniqueIdentifier;

        internal ServiceInstance(
            string resource,
            string serviceUri,
            DateTime registrationExpiresAt)
            : this(
            Guid.NewGuid(),
            resource,
            serviceUri,
            registrationExpiresAt) { }

        internal ServiceInstance(
            ServiceInstance currentInstance,
            DateTime registrationExpiresAt)
            : this(
            currentInstance.UniqueIdentifier,
            currentInstance.Resource,
            currentInstance.ServiceUri,
            registrationExpiresAt) { }

        public ServiceInstance(
            Guid uniqueIdentifier,
            string resource,
            string serviceUri,
            DateTime registrationExpiresAt)
        {
            if (ReferenceEquals(uniqueIdentifier, Guid.Empty)) throw new ArgumentNullException("uniqueIdentifier");
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUri)) throw new ArgumentNullException("serviceUri");

            _UniqueIdentifier = uniqueIdentifier;
            _Resource = resource;
            _ServiceUri = serviceUri;
            _RegistrationExpiresAt = registrationExpiresAt;
        }

        public DateTime RegistrationExpiresAt
        {
            get { return _RegistrationExpiresAt; }
        }

        public string Resource
        {
            get { return _Resource; }
        }

        public string ServiceUri
        {
            get { return _ServiceUri; }
        }

        public Guid UniqueIdentifier
        {
            get { return _UniqueIdentifier; }
        }

    }

}
