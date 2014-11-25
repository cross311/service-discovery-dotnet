using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_discovery
{
    internal class InMemoryServiceRegistryRepository : IServiceRegistryRepository
    {
        private static readonly TimeSpan _DefaultCheckInWithinTime = TimeSpan.FromMinutes(2);
        private readonly TimeSpan _CheckInWithinTime;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>> _Repository;

        public InMemoryServiceRegistryRepository()
            : this(_DefaultCheckInWithinTime) { }

        public InMemoryServiceRegistryRepository(TimeSpan checkInWithInTime)
        {
            _Repository = new ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>>();
            _CheckInWithinTime = checkInWithInTime;
        }


        public IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");

            var normalizedResource = NormalizeKey(resource);
            ConcurrentDictionary<string, ServiceInstance> instancesLookup;

            if (!_Repository.TryGetValue(normalizedResource, out instancesLookup))
                return Enumerable.Empty<ServiceInstance>();

            var validCheckInWithinTime = DateTime.UtcNow.Subtract(_CheckInWithinTime);

            var instances = instancesLookup.Values;
            var validInstances = instances.Where(instance => instance.RegistrationExpiresAt > validCheckInWithinTime);

            return validInstances;
        }

        public ServiceInstance AddOrUpdate(string resource, string serviceUriString)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUriString)) throw new ArgumentNullException("serviceUriString");


            var ticketValidForTwoMinutes = DateTime.UtcNow.Add(_CheckInWithinTime);

            var resourceNormalized = NormalizeKey(resource);

            var resourceInstances = _Repository.GetOrAdd(resourceNormalized, new ConcurrentDictionary<string, ServiceInstance>());

            var serviceUriNormalized = NormalizeKey(serviceUriString);

            var serviceInstance = resourceInstances.AddOrUpdate(
                                    serviceUriNormalized,
                                    new ServiceInstance(resourceNormalized, serviceUriNormalized, ticketValidForTwoMinutes),
                                    (_k, currentInstance) => new ServiceInstance(currentInstance, ticketValidForTwoMinutes));
            return serviceInstance;
        }


        private static string NormalizeKey(string key)
        {
            var normalized = key.ToLowerInvariant().Trim();
            return normalized;
        }
    }
}
