using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Microsoft.ServiceFabric.Actors;

namespace DeviceActor.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IDeviceActor : IActor
    {
      
        Task SetDeviceConfiguration(DeviceDetails count, CancellationToken cancellationToken);
        Task<DeviceDetails> GetDeviceConfiguration();
    }
}
