using Mujoco;
using RosMessageTypes.Std;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PubSensorData : MonoBehaviour
{
    public bool logging;
    public bool UdpSending;
    public byte SIM_SensorCategory;

    [Serializable]
    public struct SensorInfo
    {
        public GameObject GO;
        public byte Subcategory;
    }
    public List<SensorInfo> sensorList = new List<SensorInfo>();

    // Start is called before the first frame update
    void Start()
    {
        string header = "time_stamp_s" + "," + "sensor_name" + "," + "ForceX" + "," + "ForceY" + "," + "ForceZ" + Environment.NewLine;
        LoggingManager.CreateNewLog("ForceLogs", header);
    }

    // Update is called once per frame
    private void Update()
    {
        foreach (var sensorInfo in sensorList)
        {
            var sensorComp = sensorInfo.GO.GetComponent<MjSiteVectorSensor>();
            string name = sensorInfo.GO.name;

            float forceX = sensorComp.ForceX;
            float forceY = sensorComp.ForceY;
            float forceZ = sensorComp.ForceZ;
            
            if (logging)
            {
                string message = $"{StreamlinedInputManager.Now},{name},{forceX},{forceY},{forceZ}" + Environment.NewLine;
                LoggingManager.AddToBuffer("ForceLogs", message);
            }

            if (UdpSending)
            {
                SimUdpSender.SendArrayAsUDPmessage(new double[] { forceX, forceY, forceZ }, (SIM_SensorCategory, sensorInfo.Subcategory));
            }
        }
    }
}
