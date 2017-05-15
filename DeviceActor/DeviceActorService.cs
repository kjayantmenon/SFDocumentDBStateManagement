using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using DeviceActor.Interfaces;
using Microsoft.ServiceFabric.Data;

namespace DeviceActor
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class DeviceActorService : ActorService 
    {
        /// <summary>
        /// Initializes a new instance of DeviceActor
        /// </summary>
        private const string ConfigurationPackage = "Config";
        private const string ConfigurationSection = "WorkerActorCustomConfig";
        private const string QueueLengthParameter = "QueueLength";

        //public IReliableStateManager StateManager { get; set; }
        public DeviceActorService(StatefulServiceContext context,
           ActorTypeInformation typeInfo,
           Func<ActorService, ActorId, ActorBase> actorFactory,
           Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory,
           IActorStateProvider stateProvider = null,
           ActorServiceSettings settings = null)
            : base(context, typeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            // Read Settings
            //ReadSettings(context.CodePackageActivationContext.GetConfigurationPackageObject(ConfigurationPackage));

            // Creates event handlers for configuration changes
             
            context.CodePackageActivationContext.ConfigurationPackageAddedEvent +=
                ConfigPkgAdded;
            context.CodePackageActivationContext.ConfigurationPackageModifiedEvent +=
                ConfigPkgChanged;
            context.CodePackageActivationContext.ConfigurationPackageRemovedEvent +=
                ConfigPkgRemoved;
        }

        private void ConfigPkgRemoved(object sender, System.Fabric.PackageRemovedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Removed!" + e.Package.Path);
            Debugger.Break();
        }

        private void ConfigPkgAdded(object sender, System.Fabric.PackageAddedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Added!" + e.Package.Path);
            Debugger.Break();
        }

        private void ConfigPkgChanged(object sender, System.Fabric.PackageModifiedEventArgs<System.Fabric.ConfigurationPackage> e)
        {
            Debug.WriteLine("***Config Package Updated!" + e.NewPackage.Path);
            Debugger.Break();
        }

        /// <summary>
        /// TODO: Replace with your own actor method.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>

        public Task SetDeviceConfiguration(int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
