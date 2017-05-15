using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Data;

namespace DocDbStateManager
{
    public class DocDbStateManager: IActorStateManager
    {
        public DocDbStateManager()
        {
            Console.WriteLine("Created state manager.");
        }

        private readonly Dictionary<string, StateMetadata> stateChangeTracker;
        private readonly ActorBase actor;

        public DocDbStateManager(ActorBase actor)
        {
            this.actor = actor;
            this.stateChangeTracker = new Dictionary<string, StateMetadata>();
        }
        public async Task AddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            if (!(await this.TryAddStateAsync(stateName, value, cancellationToken)))
            {
                throw new InvalidOperationException($"{ActorStates.ActorStateAlreadyExists}, {stateName}");
            }

           
        }

        public async Task<T> GetStateAsync<T>(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            var condRes = await this.TryGetStateAsync<T>(stateName, cancellationToken);

            if (condRes.HasValue)
            {
                return condRes.Value;
            }

            throw new KeyNotFoundException("Actor state not found");
            //string s = "this is the state";
            //return await Task.FromResult(GetValue<T>(s));
        }

        public static T GetValue<T>(String value)
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        public Task SetStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task RemoveStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<bool> TryAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            //Requires.Argument("stateName", stateName).NotNull();

            if (this.stateChangeTracker.ContainsKey(stateName))
            {
                var stateMetadata = this.stateChangeTracker[stateName];

                // Check if the property was marked as remove in the cache
                if (stateMetadata.ChangeKind == StateChangeKind.Remove)
                {
                    this.stateChangeTracker[stateName] = StateMetadata.Create(value, StateChangeKind.Update);
                    return true;
                }

                return false;
            }
            var helper = new DocDBStateProvider();
            var client = helper.Init();
            if (await helper.ContainsStateAsync(client, this.actor, stateName))
            {
               return false;
            }else
            {
                await helper.CreateActorStateIfNotExists(client, this.actor, value);
                return true;
            }

            this.stateChangeTracker[stateName] = StateMetadata.Create(value, StateChangeKind.Add);
            return true;
        }

        public async Task<ConditionalValue<T>> TryGetStateAsync<T>(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            //Requires.Argument("stateName", stateName).NotNull();

            if (this.stateChangeTracker.ContainsKey(stateName))
            {
                var stateMetadata = this.stateChangeTracker[stateName];

                // Check if the property was marked as remove in the cache
                if (stateMetadata.ChangeKind == StateChangeKind.Remove)
                {
                    return new ConditionalValue<T>(false, default(T));
                }

                return new ConditionalValue<T>(true, (T)stateMetadata.Value);
            }

            var conditionalResult = await this.TryGetStateFromStateProvider<T>(stateName, cancellationToken);
            if (conditionalResult.HasValue)
            {
                this.stateChangeTracker.Add(stateName, StateMetadata.Create(conditionalResult.Value, StateChangeKind.None));
            }

            return conditionalResult;
        }

        private async Task<ConditionalValue<T>> TryGetStateFromStateProvider<T>(string stateName, CancellationToken cancellationToken)
        {
            ConditionalValue<T> result;

            //this.actor.Manager.DiagnosticsEventManager.LoadActorStateStart(this.actor);

            var stateProvider = new DocDBStateProvider();
            var client = stateProvider.Init();

            if (await stateProvider.ContainsStateAsync(client, this.actor, stateName))
            {
                var value = await stateProvider.LoadStateAsync<T>(client, this.actor, stateName, cancellationToken);
                result = new ConditionalValue<T>(true, value);
            }
            else
            {
                result = new ConditionalValue<T>(false, default(T));
            }

            //this.actor.Manager.DiagnosticsEventManager.LoadActorStateFinish(this.actor);
            return result;
        }

        public Task<bool> TryRemoveStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> ContainsStateAsync(string stateName, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetOrAddStateAsync<T>(string stateName, T value, CancellationToken cancellationToken = new CancellationToken())
        {
            var condRes = await this.TryGetStateAsync<T>(stateName, cancellationToken);

            if (condRes.HasValue)
            {
                return condRes.Value;
            }

            var changeKind = this.IsStateMarkedForRemove(stateName) ? StateChangeKind.Update : StateChangeKind.Add;

            this.stateChangeTracker[stateName] = StateMetadata.Create(value, changeKind);
            return value;
        }
        private bool IsStateMarkedForRemove(string stateName)
        {
            if (this.stateChangeTracker.ContainsKey(stateName) &&
                this.stateChangeTracker[stateName].ChangeKind == StateChangeKind.Remove)
            {
                return true;
            }

            return false;
        }
        public async Task<T> AddOrUpdateStateAsync<T>(string stateName, T addValue, Func<string, T, T> updateValueFactory,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //Requires.Argument("stateName", stateName).NotNull();
           
            if (this.stateChangeTracker.ContainsKey(stateName))
            {
                var stateMetadata = this.stateChangeTracker[stateName];

                // Check if the property was marked as remove in the cache
                if (stateMetadata.ChangeKind == StateChangeKind.Remove)
                {
                    this.stateChangeTracker[stateName] = StateMetadata.Create(addValue, StateChangeKind.Update);
                    return addValue;
                }

                var newValue = updateValueFactory.Invoke(stateName, (T)stateMetadata.Value);
                stateMetadata.Value = newValue;

                if (stateMetadata.ChangeKind == StateChangeKind.None)
                {
                    stateMetadata.ChangeKind = StateChangeKind.Update;
                }

                return newValue;
            }

            var conditionalResult = await this.TryGetStateFromStateProvider<T>(stateName, cancellationToken);
            if (conditionalResult.HasValue)
            {
                var newValue = updateValueFactory.Invoke(stateName, conditionalResult.Value);
                this.stateChangeTracker.Add(stateName, StateMetadata.Create(newValue, StateChangeKind.Update));

                return newValue;
            }

            this.stateChangeTracker[stateName] = StateMetadata.Create(addValue, StateChangeKind.Add);
            return addValue;
        }

        public Task<IEnumerable<string>> GetStateNamesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public async Task ClearCacheAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Console.WriteLine("Cache Cleared");
        }

        public async Task SaveStateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            
            Console.WriteLine("State Saved....");
        }
    }

    public enum ActorStates
    {
        ActorStateAlreadyExists
    }

    public sealed class StateMetadata
    {
        private readonly Type type;

        private StateMetadata(object value, Type type, StateChangeKind changeKind)
        {
            this.Value = value;
            this.type = type;
            this.ChangeKind = changeKind;
        }

        public object Value { get; set; }

        public StateChangeKind ChangeKind { get; set; }

        public Type Type
        {
            get { return this.type; }
        }

        public static StateMetadata Create<T>(T value, StateChangeKind changeKind)
        {
            return new StateMetadata(value, typeof(T), changeKind);
        }

        public static StateMetadata CreateForRemove()
        {
            return new StateMetadata(null, typeof(object), StateChangeKind.Remove);
        }
    }


    public class DocDBStateProvider //<T> where T:ActorState
    {
        
            private static string _endpointUri = "[get endpoint uri using portal or PS]";
            private static string _primaryKey = @"get primary key from the portal or using PS";
            private static string dbName = "ActorStateDB";
            private static string collectionName = "actorstatecollection";

            public DocumentClient Init()
            {
                Console.WriteLine("DocumentDB test");
                var client = CreateDocumentClient();

                client.CreateDatabaseAsync((new Database() { Id = dbName }));
                client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(dbName),
                    new DocumentCollection() { Id = collectionName });


                return client;
                //for (int i = 0; i < 5; i++)
                //{
                //    var actorState = new ActorState() { Id = i.ToString(), IsOnline = i % 2 == 0 ? true : false, Family = i % new Random().Next(1, 3) == 0 ? "MicroRaE" : "BW" };
                //    CreateActorStateIfNotExists(client, actorState);
                //}


                //Console.ReadKey();
            }

            public async Task CreateActorStateIfNotExists<T>(DocumentClient client, ActorBase actor, T actorState)
            {
                try
                {
                    string id = actor.Id.ToString();
                    await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(dbName, collectionName,id));

                }
                catch (DocumentClientException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        try
                        {
                            dynamic state = actorState;
                            
                            await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(dbName, collectionName), actorState);
                        }
                        catch (Exception ex1)
                        {
                            Console.WriteLine(ex1.ToString());
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }


        public async Task<bool> ContainsStateAsync(DocumentClient client, ActorBase actor, string actorStateName)
        {
            try
            {
                var doc = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(dbName, collectionName, actorStateName));
                return true;
            }
            catch (DocumentClientException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
                throw ex;
            }
        }

        private DocumentClient client;
        public async Task<T> LoadStateAsync<T>(DocumentClient client, ActorBase actor, string stateName, CancellationToken cancellationToken)
        {
            this.client = client;
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = 1 };

            IQueryable<T> actorStateSql = this.client.CreateDocumentQuery<T>(
                 UriFactory.CreateDocumentCollectionUri(dbName, collectionName),
                 $"select * from DeviceDetails where DeviceDetails.id = '{stateName}'",
                 queryOptions);
            foreach (T details in actorStateSql)
            {
              return details;
            }

            throw new Exception("State Not Found!");
            //IQueryable<T> familyQuery = client.CreateDocumentQuery<T>(
            //      UriFactory.CreateDocumentCollectionUri(dbName, collectionName), queryOptions).Where(f => f.Id == "Andersen"));

            //var doc = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(dbName, collectionName, stateName));
            //return (T)Convert.ChangeType(doc., typeof(T));
            
            //throw new Exception("state not found");   
        }

        private DocumentClient CreateDocumentClient()
            {

                try
                {
                    var client = new DocumentClient(new Uri(_endpointUri), _primaryKey);
                    return client;
                }
                catch (DocumentClientException ex)
                {
                    Console.WriteLine("Unable to create document client " + ex);
                    throw new DocClientCreateException(ex);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to create document client " + ex);
                    throw new DocClientCreateException(ex);
                }

            }
        }

        internal class DocClientCreateException : Exception
        {
            public Exception exception { get; private set; }
            public DocClientCreateException(Exception documentClientException)
            {
                this.exception = documentClientException;
            }
        }

    

    public class ActorState
    {
        public string Id { get; set; }
        public bool IsOnline { get; set; }
        public string Family { get; set; }
    }
}
