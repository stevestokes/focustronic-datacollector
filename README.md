# FocustronicDataCollector DataCollector - A background data collection application for Focustronic Alkatronic and Mastertronic devices

All documentation will be located within this readme. No support is provided or guaranteed.

## Logical flow

![flowdiagram](https://raw.githubusercontent.com/stevestokes/focustronic-datacollector/main/FTDC.png)

## Compatibility

Built on .NET 5.0

## Assumptions

* You should have good working knowledge of AWS
* You have an AWS account setup
* You know how to build and deploy a lambda
* You know how to deploy an RDS instance and run SQL commands
* You know how to setup an API gateway
* You have a general understanding of http requests and authentication methods
* You have a free cloud grafana account set up
* You know how to build grafan dashboards

## DataCollector Installation

* After building, add a shortcut to the DataCollector.exe in your startup folder, it will run in the background.
* Startup folder is located at: C:\Users\{username}\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
* OR
* Press Windows + R, and type shell:startup then hit enter, this will open the above folder.
* The machine running the DataCollector.exe file should be running on the same local network as your Mastertronic
* The executable will output a .log file for logging purposes
* Modify the config.json and update with your Focustronic credentials

## Lambda / AWS Installation

* Build each lambda project with the supplied build.bat
* Package the build contents into a zip file
* Upload the lambda to a function you have setup in AWS
* Suggested function names-
* alkatronic-measurements
* alkatronic-measurement-post
* measurements
* measurement-post
* Create GET and POST endpoints in API Gateway, they should point towards the correct lambda resource via AWS gateway proxy requests
* Crease a MS SQL RDS Database (small/free) and run the database.sql file to create the tables

## Building

* You can build the solution via Visual Studio Community
* Requires .NET Core SDK 5.0 to build - [download Visual Studio 2019 Community](https://www.visualstudio.com/downloads/)
* Requires Visual Studio 2019 to open the solution, [Community version](https://www.visualstudio.com/downloads/) 

## License

Licensed Under CC BY-NC-ND 2.0; you may not use this file except in 
compliance with the License. You may obtain a copy of the License
[here](https://creativecommons.org/licenses/by-nc-nd/2.0/).

## End result

![endresult](https://raw.githubusercontent.com/stevestokes/focustronic-datacollector/main/Grafana.png)