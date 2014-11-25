using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_registry_dotnet
{
    public class ServiceRegistry : IServiceRegistry
    {
        private static readonly TimeSpan _DefaultCheckInWithinTime = TimeSpan.FromMinutes(2);
        private static readonly IEnumerable<ServiceInstance> _EmptyServiceInstances = Enumerable.Empty<ServiceInstance>();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>> _ServiceRegistry;
        private readonly IServiceRegistryRepository _ServiceRegistryRepository;
        private readonly TimeSpan _CheckInWithinTime;

        public ServiceRegistry() 
            : this(_DefaultCheckInWithinTime) { }

        public ServiceRegistry(TimeSpan checkInWithinTime)
            : this(checkInWithinTime, new InMemoryServiceRegistryRepository()) { }

        public ServiceRegistry(TimeSpan checkInWithinTime, IServiceRegistryRepository serviceRegistryRepository)
        {
            if(ReferenceEquals(serviceRegistryRepository, null)) throw new ArgumentNullException("serviceRegistryRepository");

            _CheckInWithinTime = checkInWithinTime;
            _ServiceRegistryRepository = serviceRegistryRepository;
            _ServiceRegistry = new ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>>();
        }

        public RegistrationTicket Register(ServiceRegistration registration)
        {
            var resource = registration.Resource;
            var serviceUriString = registration.InstanceServiceUri;
            var healthCheckUriString = registration.InstanceHealthCheckUri;

            var invalidResponse =  ValidateRegistration(resource, serviceUriString, healthCheckUriString);

            if (!ReferenceEquals(invalidResponse, null))
            {
                return invalidResponse;
            }

            var ticketValidForTwoMinutes = DateTime.UtcNow.Add(_CheckInWithinTime);

            var resourceNormalized = NormalizeKey(resource);

            var resourceInstances = _ServiceRegistry.GetOrAdd(resourceNormalized, new ConcurrentDictionary<string, ServiceInstance>());

            var serviceUriNormalized = NormalizeKey(serviceUriString);

            var serviceInstance = resourceInstances.AddOrUpdate(
                                    serviceUriNormalized,
                                    new ServiceInstance(resourceNormalized, serviceUriNormalized, healthCheckUriString),
                                    (_k, currentInstance) => new ServiceInstance(currentInstance));

            var successResult = new RegistrationTicket(serviceInstance, ticketValidForTwoMinutes);

            return successResult;
        }

        public IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource)
        {
            if(string.IsNullOrWhiteSpace(resource))
                return _EmptyServiceInstances;

            var normalizedResource = NormalizeKey(resource);
            ConcurrentDictionary<string, ServiceInstance> instancesLookup;

            if(!_ServiceRegistry.TryGetValue(normalizedResource, out instancesLookup))
                return _EmptyServiceInstances;

            var validCheckInWithinTime = DateTime.UtcNow.Subtract(_CheckInWithinTime);

            var instances = instancesLookup.Values;
            var validInstances = instances.Where(instance => instance.LastValidHealthCheck > validCheckInWithinTime);
            return validInstances;
        }

        private static string NormalizeKey(string key)
        {
            var normalized = key.ToLowerInvariant().Trim();
            return normalized;
        }

        private static RegistrationTicket ValidateRegistration(string resource, string serviceUriString, string healthCheckUriString)
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

            if (string.IsNullOrWhiteSpace(healthCheckUriString))
            {
                var healthCheckUriNullResponse = new RegistrationTicket(RegistrationFailReasons.InstanceHealthCheckUriMustNotBeEmpty, resource, serviceUriString);
                return healthCheckUriNullResponse;
            }

            if (!Uri.IsWellFormedUriString(serviceUriString, UriKind.Absolute))
            {
                var serviceUriNotValid = new RegistrationTicket(RegistrationFailReasons.InstanceServiceUriMustBeValidUri, resource, serviceUriString);
                return serviceUriNotValid;
            }

            if (!Uri.IsWellFormedUriString(healthCheckUriString, UriKind.Absolute))
            {
                var healthCheckUriNotValid = new RegistrationTicket(RegistrationFailReasons.InstanceHealthCheckUriMustBeValidUri, resource, serviceUriString);
                return healthCheckUriNotValid;
            }

            return null;
        }
    }

    public class ServiceRegistration
    {
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly string _InstanceHealthCheckUri;

        public ServiceRegistration(string resource, string instanceServiceUri, string instanceHealthCheckUri)
        {
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _InstanceHealthCheckUri = instanceHealthCheckUri;
        }

        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public string InstanceHealthCheckUri { get { return _InstanceHealthCheckUri; } }
    }

    public class RegistrationTicket
    {
        private readonly bool _Success;
        private readonly RegistrationFailReasons _FailReason;
        private readonly Guid _InstanceRegistrationUniqueIdentifier;
        private readonly string _Resource;
        private readonly string _InstanceServiceUri;
        private readonly DateTime _TicketGoodUntil;

        public RegistrationTicket(
            ServiceInstance serviceInstance,
            DateTime ticketGoodUntil)
            :this(
            true,
            RegistrationFailReasons.None,
            serviceInstance.UniqueIdentifier,
            serviceInstance.Resource,
            serviceInstance.ServiceUri,
            ticketGoodUntil)
        {
        }

        public RegistrationTicket(
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
            DateTime ticketGoodUntil)
        {
            _Success = success;
            _FailReason = failReason;
            _InstanceRegistrationUniqueIdentifier = instanceRegistrationUniqueIdentifier;
            _Resource = resource;
            _InstanceServiceUri = instanceServiceUri;
            _TicketGoodUntil = ticketGoodUntil;
        }

        public bool Success { get { return _Success; } }
        public RegistrationFailReasons FailReason { get { return _FailReason; } }
        public Guid InstanceRegistrationUniqueIdentifier { get { return _InstanceRegistrationUniqueIdentifier; } }
        public string Resource { get { return _Resource; } }
        public string InstanceServiceUri { get { return _InstanceServiceUri; } }
        public DateTime TicketGoodUntil { get { return _TicketGoodUntil; } }
    }

    public enum RegistrationFailReasons
    {
        None,
        ResourceMustNotBeEmpty,
        InstanceServiceUriMustNotBeEmpty,
        InstanceServiceUriMustBeValidUri,
        InstanceHealthCheckUriMustNotBeEmpty,
        InstanceHealthCheckUriMustBeValidUri
    }

    public class ServiceInstance
    {
        private readonly string _Resource;
        private readonly string _ServiceUri;
        private readonly string _HealthCheckUri;
        private readonly DateTime _LastValidHealthCheck;
        private readonly Guid _UniqueIdentifier;

        public ServiceInstance(
            string resource,
            string serviceUri,
            string healthCheckUri)
            : this(
            Guid.NewGuid(),
            resource,
            serviceUri,
            healthCheckUri) { }

        public ServiceInstance(
            ServiceInstance currentInstance)
            : this(
            currentInstance.UniqueIdentifier,
            currentInstance.Resource,
            currentInstance.ServiceUri,
            currentInstance.HealthCheckUri) { }

        private ServiceInstance(
            Guid uniqueIdentifier,
            string resource,
            string serviceUri,
            string healthCheckUri)
        {
            _UniqueIdentifier = uniqueIdentifier;
            _Resource = resource;
            _ServiceUri = serviceUri;
            _HealthCheckUri = healthCheckUri;
            _LastValidHealthCheck = DateTime.UtcNow;
        }

        public DateTime LastValidHealthCheck
        {
            get { return _LastValidHealthCheck; }
        }

        public string Resource
        {
            get { return _Resource; }
        }

        public string HealthCheckUri
        {
            get { return _HealthCheckUri; }
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
