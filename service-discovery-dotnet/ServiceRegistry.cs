using System;
using System.Collections.Generic;
using System.Linq;

namespace service_discovery
{
    public class ServiceRegistry : IServiceRegistry
    {
        private static readonly IEnumerable<ServiceInstance> _EmptyServiceInstances = Enumerable.Empty<ServiceInstance>();
        private readonly IServiceRegistryRepository _ServiceRegistryRepository;

        public ServiceRegistry() 
            : this(new InMemoryServiceRegistryRepository()) { }

        internal ServiceRegistry(IServiceRegistryRepository serviceRegistryRepository)
        {
            if(ReferenceEquals(serviceRegistryRepository, null)) throw new ArgumentNullException("serviceRegistryRepository");

            _ServiceRegistryRepository = serviceRegistryRepository;
        }

        public RegistrationTicket Register(ServiceRegistration registration)
        {
            var resource = registration.Resource;
            var serviceUriString = registration.InstanceServiceUri;
            var timeToLive = registration.TimeToLive;

            var invalidResponse = ValidateRegistration(resource, serviceUriString, timeToLive);

            if (!ReferenceEquals(invalidResponse, null))
            {
                return invalidResponse;
            }

            var registrationExpiresAt = RegistrationExpiresAt(timeToLive);
            var serviceInstance = _ServiceRegistryRepository.AddOrUpdate(resource, serviceUriString, registrationExpiresAt);

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

        private static DateTime RegistrationExpiresAt(TimeSpan timeToLive)
        {
            if (timeToLive == TimeSpan.MaxValue)
                return DateTime.MaxValue;

            var utcNow = DateTime.UtcNow;

            if ((DateTime.MaxValue - utcNow) < timeToLive)
                return DateTime.MaxValue;

            return utcNow.Add(timeToLive);
        }

        private static RegistrationTicket ValidateRegistration(string resource, string serviceUriString, TimeSpan timeToLive)
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

            if (timeToLive <= TimeSpan.Zero)
            {
                var timeToLiveNotValid = new RegistrationTicket(RegistrationFailReasons.TimeToLiveMustBeGreaterThanZero, resource, serviceUriString);
                return timeToLiveNotValid;
            }

            return null;
        }
    }

    public class ServiceRegistration
    {
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly TimeSpan _TimeToLive;

        public ServiceRegistration(string resource, string instanceServiceUri)
            : this(resource, instanceServiceUri, TimeSpan.MaxValue)
        {
        }

        public ServiceRegistration(string resource, string instanceServiceUri, TimeSpan timeToLive)
        {
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _TimeToLive = timeToLive;
        }

        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public TimeSpan TimeToLive { get { return _TimeToLive; } }
    }

    public class RegistrationTicket
    {
        private readonly bool _Success;
        private readonly RegistrationFailReasons _FailReason;
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly DateTime _RegistrationExpiresAt;

        internal RegistrationTicket(
            ServiceInstance serviceInstance)
            :this(
            true,
            RegistrationFailReasons.None,
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
                resource,
                instanceServiceUri,
                DateTime.MinValue)
        {
        }


        private RegistrationTicket(
            bool success,
            RegistrationFailReasons failReason, 
            string resource,
            string instanceServiceUri, 
            DateTime registrationExpiresAt)
        {
            _Success = success;
            _FailReason = failReason;
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _RegistrationExpiresAt = registrationExpiresAt;
        }

        public bool Success { get { return _Success; } }
        public RegistrationFailReasons FailReason { get { return _FailReason; } }
        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public DateTime RegistrationExpiresAt { get { return _RegistrationExpiresAt; } }
    }

    public enum RegistrationFailReasons
    {
        None,
        ResourceMustNotBeEmpty,
        InstanceServiceUriMustNotBeEmpty,
        InstanceServiceUriMustBeValidUri,
        TimeToLiveMustBeGreaterThanZero
    }

    public class ServiceInstance
    {
        private readonly string _Resource;
        private readonly string _ServiceUri;
        private readonly DateTime _RegistrationExpiresAt;

        internal ServiceInstance(
            ServiceInstance currentInstance,
            DateTime registrationExpiresAt)
            : this(
            currentInstance.Resource,
            currentInstance.ServiceUri,
            registrationExpiresAt) { }

        public ServiceInstance(
            string resource,
            string serviceUri,
            DateTime registrationExpiresAt)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUri)) throw new ArgumentNullException("serviceUri");

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

    }

}
