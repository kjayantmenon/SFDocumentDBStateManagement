using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using DocDbStateManager;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace DeviceActor
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform

                
               var settings = new ActorGarbageCollectionSettings(300, 60);
                ActorRuntime.RegisterActorAsync<DeviceActor>(
                    (context, actorType) => new DeviceActorService(context,
                        actorType,
                        (s, i) => new DeviceActor(s, i),
                        (ab, sp)=> new DocDbStateManager.DocDbStateManager(ab),
                        null,
                        new ActorServiceSettings
                        {
                            ActorGarbageCollectionSettings = settings
    })).GetAwaiter().GetResult();


            Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
