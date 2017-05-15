using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using DeviceActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Generator;

namespace WebApi.Controllers
{
    public class DeviceController:ApiController
    {
        [HttpPost]
        [Route("api/devices/add/{deviceId}")]
        public async Task<IHttpActionResult> CreateNewDevice([FromUri] string deviceId,HttpRequestMessage requestMessage)
        {

            try
            {
                var json = await requestMessage.Content.ReadAsStringAsync();
                dynamic request = JObject.Parse(json);

                JObject jSessionDetails = request.sessionDetails;
                JObject jDeviceDetails = request.deviceDetails;

                var sessionDetails = jSessionDetails.ToObject<SessionDetails>();
                var deviceDetails = jDeviceDetails.ToObject<DeviceDetails>();
                string ApplicationName = "testv1";
                Uri actorUri =  ActorNameFormat.GetFabricServiceUri(typeof(IDeviceActor), ApplicationName);

                IDeviceActor device = ActorProxy.Create<IDeviceActor>(new ActorId(deviceId), actorUri);
                DeviceDetails details = new DeviceDetails() {MacAddress = deviceDetails.MacAddress, SensorCount = deviceDetails.SensorCount, SerialNumber = deviceDetails .SerialNumber};
                await device.SetDeviceConfiguration(details, new CancellationToken());

            }
            catch (Exception ex)
            {

                throw ex;
            }    
            //SessionDetails sessionDetails, [FromBody] DeviceDetails deviceDetails
            return Ok();
        }


        [HttpPost]
        [Route("api/devices/update/{deviceId}")]
        public async Task<IHttpActionResult> UpdateDevice([FromUri] string deviceId, HttpRequestMessage requestMessage)
        {
            DeviceDetails details = null;
            
            try
            {
                var json = await requestMessage.Content.ReadAsStringAsync();
                dynamic request = JObject.Parse(json);

                JObject jSessionDetails = request.sessionDetails;
                JObject jDeviceDetails = request.deviceDetails;

                var sessionDetails = jSessionDetails.ToObject<SessionDetails>();
                var deviceDetails = jDeviceDetails.ToObject<DeviceDetails>();
                string ApplicationName = "testv1";
                Uri actorUri = ActorNameFormat.GetFabricServiceUri(typeof(IDeviceActor), ApplicationName);

                IDeviceActor device = ActorProxy.Create<IDeviceActor>(new ActorId(deviceId), actorUri);
                details = new DeviceDetails()
                {
                    MacAddress = deviceDetails.MacAddress,
                    SensorCount = deviceDetails.SensorCount,
                    SerialNumber = deviceDetails.SerialNumber
                };
                await device.SetDeviceConfiguration(details, new CancellationToken());

            }
            catch (Exception ex)
            {

                throw ex;
            }
            return Ok("Device Details updated");
        }

        public async Task<DeviceDetails> GetDeviceDetails([FromUri] string deviceId)
        {
            string ApplicationName = "testv1";
            Uri actorUri = ActorNameFormat.GetFabricServiceUri(typeof(IDeviceActor), ApplicationName);

            DeviceDetails deviceDetails = null;//ActorProxy .Create<IDeviceActor>(new ActorId(deviceId), actorUri); ;
            return deviceDetails;
        }
    }


}
