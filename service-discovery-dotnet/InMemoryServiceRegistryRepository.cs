using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace service_discovery
{
    internal class InMemoryServiceRegistryRepository : IServiceRegistryRepository
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>> _Repository;

        public InMemoryServiceRegistryRepository()
        {
            _Repository = new ConcurrentDictionary<string, ConcurrentDictionary<string, ServiceInstance>>();
        }


        public IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");

            var normalizedResource = NormalizeKey(resource);
            ConcurrentDictionary<string, ServiceInstance> instancesLookup;

            if (!_Repository.TryGetValue(normalizedResource, out instancesLookup))
                return Enumerable.Empty<ServiceInstance>();

            var utcNow = DateTime.UtcNow;

            var instances = instancesLookup.Values;
            var validInstances = instances.Where(instance => instance.RegistrationExpiresAt > utcNow);

            return validInstances;
        }

        public ServiceInstance AddOrUpdate(string resource, string serviceUriString, IEnumerable<string> tags, DateTime registrationExperation)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUriString)) throw new ArgumentNullException("serviceUriString");
            if (registrationExperation == DateTime.MinValue) throw new ArgumentOutOfRangeException("registrationExperation");
            if(ReferenceEquals(tags, null)) throw new ArgumentNullException("tags");


            var resourceNormalized = NormalizeKey(resource);

            var resourceInstances = _Repository.GetOrAdd(resourceNormalized, new ConcurrentDictionary<string, ServiceInstance>());

            var serviceUriNormalized = NormalizeKey(serviceUriString);
            var tagsNormalized = tags.Select(NormalizeKey).ToList();
            var tagsReadonly = new ReadOnlyCollection<string>(tagsNormalized);

            var serviceInstance = new ServiceInstance(resourceNormalized, serviceUriNormalized, tagsReadonly, registrationExperation);
            serviceInstance = resourceInstances.AddOrUpdate(
                                    serviceUriNormalized,
                                    serviceInstance,
                                    (_k, _v) => serviceInstance);
            return serviceInstance;
        }


        private static string NormalizeKey(string key)
        {
            var normalized = key.ToLowerInvariant().Trim();
            return normalized;
        }
    }
}
