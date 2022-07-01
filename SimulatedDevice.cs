// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using System.Net.Http;
using System.Collections.Generic;

namespace simulatedDevice
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        private const string s_connectionString = "HostName=Practice-iot.azure-devices.net;DeviceId=thermostat-dev;SharedAccessKey=6HL+302MPnZWjn+XQ6YM+isuOLVaQDBues6qLydH2jg=";
        private const string s_serviceConnectionString = "HostName=Practice-iot.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dc30cfudaho6rMgMaEiHVcsv73Z0cx4O+9QxG7oXKBg=";
        private static Boolean ACOn = false;
        private static Boolean heaterOn = false;
        static double maxTemperature = 80;
        static double minTemperature = 55;
        private static double currentTemperature;
        // Async method to send simulated telemetry

        private static void generateThermostatInfo()
        {
            Random rand = new Random();

            if (currentTemperature > maxTemperature)
            {
                ACOn = true;
                //SendInfoToAzureFunctionAsync();
            }
            else
            {
                ACOn = false;
            }

            if (currentTemperature < minTemperature)
            {
                heaterOn = true;
            }
            else
            {
                heaterOn = false;
            }

            currentTemperature = minTemperature + rand.NextDouble() * 15;

            while (currentTemperature > maxTemperature || currentTemperature < minTemperature)
            {
                rand = new Random();
                currentTemperature = minTemperature + rand.NextDouble() * 15;
            }
        }
        private static async void SendThermoInfoToIOTHubAsync()
        {
            // Initial telemetry values

            // Create JSON message       
            var telemetryDataPoint = new
            {
                temperature = currentTemperature,
                maxTemperature = maxTemperature,
                minTemperature = minTemperature,
                ac_on = ACOn,
                heater_on = heaterOn
            };

            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Microsoft.Azure.Devices.Client.Message(Encoding.ASCII.GetBytes(messageString));
            message.ContentType = "application/json";
            message.ContentEncoding = "utf-8";


            // Add a custom application property to the message.
            // An IoT hub can filter on these properties without access to the message body.
            message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

            // Send the tlemetry message
            await s_deviceClient.SendEventAsync(message).ConfigureAwait(false);
            Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

            await Task.Delay(1000).ConfigureAwait(false);

        }

        // private async static Task SendInfoToAzureFunctionAsync()
        // {
        //     //requires a "temperature"
        //     HttpClient client = new HttpClient();
        //     string url = "https://practice-function.azurewebsites.net/api/HttpTrigger1?temperature=";
            
        //     //PostAsync requires two values with last beign
        //     //an encodedContent variable. Can't just have the 
        //     //info added to the url string. JSon message doesn't work
        //     //for encodedContent so use Dictionary

        //     // var info = new Dictionary<string, string>
        //     // {
        //     //     { "temperature", currentTemperature.ToString() }
        //     // };
        //     // var postCommand = new FormUrlEncodedContent(info);

        //     //getAsync only requires the url with search index
        //     var response = await client.GetAsync(url + currentTemperature);
        //     string responseString = await response.Content.ReadAsStringAsync();
            
        //     Console.WriteLine(responseString);
        //     System.Threading.Thread.Sleep(20000);
        // }

        private async static Task SendInfoToUserAsync()
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_serviceConnectionString);
            string targetDevice = "iotdev1";
            string message = $"Current temperature: {currentTemperature}, AC status: {ACOn}, heater status: {heaterOn}";
            Console.WriteLine(message);
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes(message));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }
        private static async void ReceiveUserMessageAsync()
        {
            //Console.WriteLine("\nReceiving cloud to device messages from service");
            bool continueLoop = true;
            while (true)
            {
                Microsoft.Azure.Devices.Client.Message receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null)
                {
                    continue;
                }
                else
                {
                    continueLoop = false;
                }
                Console.ForegroundColor = ConsoleColor.Yellow;
                string message = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Received message: {0}", message);


                setTemperature(message);
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }

        private static void setTemperature(string message)
        {
            //format "Set max/minTemp to: ##"
            string[] temp = message.Split(' ');
            if (temp[1].Equals("minTemp"))
            {
                minTemperature = double.Parse(temp[temp.Length - 1]);
            }
            else if (temp[1].Equals("maxTemp"))
            {
                maxTemperature = double.Parse(temp[temp.Length - 1]);
            }
        }

        private static void Main()
        {
            //Console.WriteLine("IoT Hub Quickstarts - Simulated device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);

            currentTemperature = 90;
            generateThermostatInfo();

            while (true)
            {
                ReceiveUserMessageAsync();
                generateThermostatInfo();
                SendInfoToUserAsync();
                //fix later v
                SendThermoInfoToIOTHubAsync();

                //1s = 1000
                System.Threading.Thread.Sleep(20000);
            }

            Console.ReadLine();
        }
    }
}
