using System;
using System.Collections.Generic;

namespace service_registry_dotnet
{
    public interface IServiceRegistry
    {
        IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource);
        RegistrationTicket Register(ServiceRegistration registration);
    }
}
