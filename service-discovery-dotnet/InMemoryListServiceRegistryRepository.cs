using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_discovery
{
    internal class InMemoryListServiceRegistryRepository : IServiceRegistryRepository
    {
        private const string _ResourceServiceUriSeperator = "::";
        private readonly ConcurrentDictionary<string, ServiceInstance> _Repository;

        public InMemoryListServiceRegistryRepository()
        {
            _Repository = new ConcurrentDictionary<string, ServiceInstance>();
        }

        public IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource, IEnumerable<string> tags)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (ReferenceEquals(tags, null)) throw new ArgumentNullException("tags");

            var resourcePartOfKey = BuildResourceInstanceKeyPart(resource);

            var instancesKeysForResource = _Repository.Keys.Where(key => key.StartsWith(resourcePartOfKey, StringComparison.InvariantCultureIgnoreCase));

            var utcNow = DateTime.UtcNow;

            foreach (var instancekey in instancesKeysForResource)
            {
                var serviceInstance = _Repository[instancekey];

                if (serviceInstance.RegistrationExpiresAt <= utcNow)
                    continue;

                // All will return true if empty filter tags
                var matchesTagFilter = tags.All(tag => serviceInstance.Tags.Any(instanceTag => instanceTag.Equals(tag, StringComparison.InvariantCultureIgnoreCase)));

                if (matchesTagFilter)
                    yield return serviceInstance;
            }
        }

        private static string BuildResourceInstanceKeyPart(string resource)
        {
            return resource + _ResourceServiceUriSeperator;
        }

        public ServiceInstance AddOrUpdate(string resource, string serviceUriString, IEnumerable<string> tags, DateTime registrationExperation)
        {
            if (string.IsNullOrWhiteSpace(resource)) throw new ArgumentNullException("resource");
            if (string.IsNullOrWhiteSpace(serviceUriString)) throw new ArgumentNullException("serviceUriString");
            if (ReferenceEquals(tags, null)) throw new ArgumentNullException("tags");
            if (registrationExperation == DateTime.MinValue) throw new ArgumentOutOfRangeException("registrationExperation");

            var serviceInstance = new ServiceInstance(resource, serviceUriString, new ReadOnlyCollection<string>(tags.ToList()), registrationExperation);

            var key = BuildInstanceKey(serviceInstance);

            var inMemoryServiceInstance = _Repository.AddOrUpdate(key, serviceInstance, (_k, _v) => serviceInstance);

            return inMemoryServiceInstance;
        }

        private static string BuildInstanceKey(ServiceInstance serviceInstance)
        {
            return string.Format("{0}{2}{1}", serviceInstance.Resource, serviceInstance.ServiceUri, _ResourceServiceUriSeperator);
        }
    }
}
