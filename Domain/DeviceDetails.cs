using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    [DataContract]
    public class DeviceDetails
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string SerialNumber { get; set; }
        [DataMember]
        public string MacAddress { get; set; }
        [DataMember]
        public string SensorCount { get; set; }
        
    }
}
