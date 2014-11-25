using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            var invalidResponse = ValidateRegistration(registration);

            if (!ReferenceEquals(invalidResponse, null))
            {
                return invalidResponse;
            }

            var resource = registration.Resource;
            var serviceUriString = registration.InstanceServiceUri;
            var timeToLive = registration.TimeToLive;
            var tags = registration.InstanceTags;

            var registrationExpiresAt = RegistrationExpiresAt(timeToLive);
            var serviceInstance = _ServiceRegistryRepository.AddOrUpdate(resource, serviceUriString, tags, registrationExpiresAt);

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

        private static RegistrationTicket ValidateRegistration(ServiceRegistration registration)
        {
            if(ReferenceEquals(registration, null)) throw new ArgumentNullException("registration");

            if (string.IsNullOrWhiteSpace(registration.Resource))
            {
                var resourceNullResponse = new RegistrationTicket(RegistrationFailReasons.ResourceMustNotBeEmpty, registration.Resource, registration.InstanceServiceUri, registration.InstanceTags);
                return resourceNullResponse;
            }

            if (string.IsNullOrWhiteSpace(registration.InstanceServiceUri))
            {
                var serviceUriNullResponse = new RegistrationTicket(RegistrationFailReasons.InstanceServiceUriMustNotBeEmpty, registration.Resource, registration.InstanceServiceUri, registration.InstanceTags);
                return serviceUriNullResponse;
            }

            if (!Uri.IsWellFormedUriString(registration.InstanceServiceUri, UriKind.Absolute))
            {
                var serviceUriNotValid = new RegistrationTicket(RegistrationFailReasons.InstanceServiceUriMustBeValidUri, registration.Resource, registration.InstanceServiceUri, registration.InstanceTags);
                return serviceUriNotValid;
            }

            if (registration.TimeToLive <= TimeSpan.Zero)
            {
                var timeToLiveNotValid = new RegistrationTicket(RegistrationFailReasons.TimeToLiveMustBeGreaterThanZero, registration.Resource, registration.InstanceServiceUri, registration.InstanceTags);
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
        private readonly IEnumerable<string> _InstanceTags;

        public ServiceRegistration(string resource, string instanceServiceUri, IEnumerable<string> instanceTags = null)
            : this(resource, instanceServiceUri, TimeSpan.MaxValue, instanceTags)
        {
        }

        public ServiceRegistration(string resource, string instanceServiceUri, TimeSpan timeToLive, IEnumerable<string> instanceTags = null)
        {
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _TimeToLive = timeToLive;
            _InstanceTags = instanceTags ?? Enumerable.Empty<string>();
        }

        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public TimeSpan TimeToLive { get { return _TimeToLive; } }
        public IEnumerable<string> InstanceTags { get { return _InstanceTags; } }
    }

    public class RegistrationTicket
    {
        private readonly bool _Success;
        private readonly RegistrationFailReasons _FailReason;
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly IReadOnlyCollection<string> _InstanceTags;
        private readonly DateTime _RegistrationExpiresAt;

        internal RegistrationTicket(
            ServiceInstance serviceInstance)
            :this(
            true,
            RegistrationFailReasons.None,
            serviceInstance.Resource,
            serviceInstance.ServiceUri,
            serviceInstance.Tags,
            serviceInstance.RegistrationExpiresAt)
        {
        }

        internal RegistrationTicket(
            RegistrationFailReasons failReason,
            string resource,
            string instanceServiceUri,
            IEnumerable<string> instanceTags)
            : this(
                false,
                failReason,
                resource,
                instanceServiceUri,
                instanceTags,
                DateTime.MinValue)
        {
        }


        private RegistrationTicket(
            bool success,
            RegistrationFailReasons failReason, 
            string resource,
            string instanceServiceUri,
            IEnumerable<string> instanceTags, 
            DateTime registrationExpiresAt)
        {
            _Success = success;
            _FailReason = failReason;
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _InstanceTags = new ReadOnlyCollection<string>(instanceTags.ToList());
            _RegistrationExpiresAt = registrationExpiresAt;
        }

        public bool Success { get { return _Success; } }
        public RegistrationFailReasons FailReason { get { return _FailReason; } }
        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public DateTime RegistrationExpiresAt { get { return _RegistrationExpiresAt; } }
        public IReadOnlyCollection<string> InstanceTags { get { return _InstanceTags; } }
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
        private readonly IReadOnlyCollection<string> _Tags;

        internal ServiceInstance(
            ServiceInstance currentInstance,
            DateTime registrationExpiresAt)
            : this(
            currentInstance.Resource,
            currentInstance.ServiceUri,
            currentInstance.Tags,
            registrationExpiresAt) { }

        public ServiceInstance(
            string resource,
            string serviceUri,
            IReadOnlyCollection<string> tags,
            DateTime registrationExpiresAt)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUri)) throw new ArgumentNullException("serviceUri");
            if(ReferenceEquals(tags, null)) throw new ArgumentNullException("tags");

            _Resource = resource;
            _ServiceUri = serviceUri;
            _Tags = tags;
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

        public IReadOnlyCollection<string> Tags
        {
            get { return _Tags; }
        }
    }

}
