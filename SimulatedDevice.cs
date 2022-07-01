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

namespace simulatedDevice
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        private const string s_connectionString = "HostName=Practice-iot.azure-devices.net;DeviceId=iotdev1;SharedAccessKey=T5ulhSt+S8nvL9dAHbjIdClDYvMhtUrrKXnq/IvPVoM=";
        private const string s_serviceConnectionString = "HostName=Practice-iot.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dc30cfudaho6rMgMaEiHVcsv73Z0cx4O+9QxG7oXKBg=";
        private static double currentTemperature;
        private static Boolean ACOn;
        private static Boolean heaterOn;
        private static double maxTemperature = 80;
        private static double minTemperature = 55;
        private static double userTemp;
        private static async void ReceiveThemostatMessageAsync(){
            bool continueLoop = true;

            while(continueLoop){
                Microsoft.Azure.Devices.Client.Message recievedMessage = await s_deviceClient.ReceiveAsync();
                if(recievedMessage ==null){
                    continue;
                } else{
                    continueLoop = false;
                }
                string message = Encoding.ASCII.GetString(recievedMessage.GetBytes());
                setCurrentTemp(message);

                await s_deviceClient.CompleteAsync(recievedMessage);
            }
        }

        private async static void setCurrentTemp(string message){
            //format "current temp: ##, ac status: t/f, heater status: t/f"
            string[] lines = message.Split(',');
            string[] temp = lines[0].Split(' ');
            currentTemperature = double.Parse(temp[temp.Length-1]);

            string[] ac = lines[1].Split(' ');
            ACOn = Boolean.Parse(ac[ac.Length-1]);
            
            string[] heater = lines[2].Split(' ');
            heaterOn = Boolean.Parse(heater[ac.Length-1]);
        }
        private async static Task SendMaxTempMessageAsync(double userTemp)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_serviceConnectionString);
            string targetDevice = "thermostat-dev";
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes("Set maxTemp to: " +userTemp));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }

        private async static Task SendMinTempMessageAsync(double userTemp)
        {
            ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(s_serviceConnectionString);
            string targetDevice = "thermostat-dev";
            var commandMessage = new Microsoft.Azure.Devices.Message(Encoding.ASCII.GetBytes("Set minTemp to: " +userTemp));
            await serviceClient.SendAsync(targetDevice, commandMessage);
        }

        private static void loadMenu(){
            Console.WriteLine("Welcome User");
            Console.WriteLine($"Current temperature: {currentTemperature}");
            Console.WriteLine($"AC status: {ACOn}\nHeater status: {heaterOn}");
            Console.WriteLine($"Max Temp: {maxTemperature}");
            Console.WriteLine($"Min Temp: {minTemperature}");
            Console.WriteLine("Refresh with R | Exit with C\nType max or min to set the temperature");
        }
        private static void Main()
        {
            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            //bool flag = false;
            string input;
            //SendDeviceToCloudMessagesAsync();
            
            
            while(true){
                //gets the currentTemp and ac status from themostat device
                //if message is null it will break out of ReceiveMessage while loop and jump back to main
                //if ReveiveMessage is put in a loop and exeption will be thrown
                
                Console.WriteLine("Connecting to themostat");
                while(currentTemperature == 0){
                    ReceiveThemostatMessageAsync();
                }

                Console.Clear();
                loadMenu();
                input = Console.ReadLine();
               
                if(input.Equals("r") || input.Equals("R")){//refresh
                    Console.Clear();
                    continue;
                }else if(input.Equals("c") || input.Equals("C")){//exit
                    break;
                }else if(input.Equals("max") || input.Equals("Max")){//max
                    while(true){ 
                        Console.WriteLine("Input the max temperature:");
                        input = Console.ReadLine();

                        //checks for valid input
                        if (double.TryParse(input, out userTemp))
                        {
                            maxTemperature = userTemp;
                            SendMaxTempMessageAsync(maxTemperature);
                            break;
                        }else{
                            Console.Clear();
                            loadMenu();
                            Console.WriteLine("\n!!Please insert a number!!");
                        }
                    }                   
                }else if(input.Equals("min") || input.Equals("Min")){//min
                    while(true){ 
                        Console.WriteLine("Input the min temperature:");
                        input = Console.ReadLine();

                        //checks for valid input
                        if (double.TryParse(input, out userTemp))
                        {
                            minTemperature = userTemp;
                            SendMinTempMessageAsync(minTemperature);
                            break;
                        }else{
                            Console.Clear();
                            loadMenu();
                            Console.WriteLine("\n!!Please insert a number!!");
                        }
                    } 
                }
            }//while
        }//main
    }
}
