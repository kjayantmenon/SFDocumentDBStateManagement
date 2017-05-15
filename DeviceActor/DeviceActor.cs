using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceActor.Interfaces;
using Domain;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace DeviceActor
{
    class DeviceActor:Actor, IDeviceActor
    {
        public DeviceDetails details { get; private set; }
        public DeviceActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            if(details==null)
                details = new DeviceDetails();

            if (this.details.id == null)
                this.details.id = actorId.ToString();
        }

        
        

        public async Task SetDeviceConfiguration(DeviceDetails details, CancellationToken cancellationToken)
        {

            this.details = details;
            details.id = this.GetActorId().ToString();
            await StateManager.GetOrAddStateAsync(Id.ToString(), this.details, cancellationToken);
            //await StateManager.AddStateAsync(Id.ToString(),this.details, cancellationToken);

        }

        public async Task<DeviceDetails> GetDeviceConfiguration()
        {
            throw new NotImplementedException();
        }

        

        protected override async Task OnActivateAsync()
        {
            details=new DeviceDetails();
            string stateName = Id.ToString();
            DeviceDetails deviceDetails = null;
            //this.TryGetStateAsync<T>(stateName, cancellationToken);
            var deviceDetailsVal = await StateManager.TryGetStateAsync<DeviceDetails>(stateName, new CancellationToken());
            
            if (deviceDetailsVal.HasValue)
            {
                deviceDetails = deviceDetailsVal.Value;
            }
            ActorEventSource.Current.Message($"Worker Actor [{Id}] activated.");
        }
    }
}
