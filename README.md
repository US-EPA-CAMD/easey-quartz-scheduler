# `Easey-Quartz-Scheduler`

[![GitHub](https://img.shields.io/github/license/US-EPA-CAMD/easey-ecmps-ui)](https://github.com/US-EPA-CAMD/easey-ecmps-ui/blob/develop/LICENSE)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=US-EPA-CAMD_easey-quartz-scheduler&metric=alert_status)](https://sonarcloud.io/dashboard?id=US-EPA-CAMD_easey-quartz-scheduler)
[![Develop Branch Pipeline](https://github.com/US-EPA-CAMD/easey-quartz-scheduler/workflows/Develop%20Branch%20Workflow/badge.svg)](https://github.com/US-EPA-CAMD/easey-ecmps-ui/actions)<br>
Quartz Job Scheduler

## Setup
	
### Scheduler

	1) Clone the easey-quartz-scheduler repository
	2) Set the current working directory to the easey-quartz-scheduler/admin
	3) Run `dotnet restore` in the command line
	4) Run `dotnet run` to launch the application [The database connection MUST also be running]

These setup steps will launch the quartz-scheduler locally, and will optionally fire up the user interface if the ```EASEY_QUARTZ_SCHEDULER_DISPLAY_UI``` environment  variable has been set to true.

### Check Engine

The Check Engine is a legacy C# process that performs an evaluation process / populates evaluation logs for Monitor Plan, QA, and Emission files. Due to the complexity of the Check Engine, debugging this process is typically done through a standalone project included in the source bundle called ```CheckEngineRunner```.  This .NET project serves a debug entry-point for running evaluations quickly. In order to launch the ```CheckEngineRunner```:

	1) Set the current working directory to CheckEngine/CheckEngineRunner
	2) Modify your debug profile accordingly to configure the debugger entrypoint
	3) Execute donet build 
	4) Start the debugger

### User Interface

The Quartz Job Scheduler includes a user interface. The UI [Forked from a public repository called Silkier Quartz] is a wrapper that is built around Quartz.net, and enables developers and CAMD admins to schedule, create, and delete existing jobs. In order to enable the UI, the ```EASEY_QUARTZ_SCHEDULER_DISPLAY_UI``` environment variable needs to be toggled to True. 

The UI is located on the ```/quartz```  route. Upon launching the UI, a valid cdx login is required to sign in. Upon authentication, quartz job instances can be created, edited, scheduled, and deleted from the UI. 

## Environment Variables

Environment variables need to be set in order to run the application properly. Most environment variables have default values, but EASEY_AUTH_API, EASEY_QUARTZ_SCHEDULER_EMAIL, EASEY_QUARTZ_SCHEDULER_SMTP_HOST, and EASEY_QUARTZ_SCHEDULER_SMTP_PORT need to be manually set.

REST API URL's required by the application...

- EASEY_AUTH_API: https://((apiHost))/api/auth-mgmt

Other application environment variables:

- EASEY_QUARTZ_SCHEDULER_TITLE: ((title))
- EASEY_QUARTZ_SCHEDULER_HOST: ((host))
- EASEY_QUARTZ_SCHEDULER_ENV: ((environment))
- EASEY_QUARTZ_SCHEDULER_EMAIL: ((email))
- EASEY_QUARTZ_SCHEDULER_SMTP_HOST: ((smtpHost))
- EASEY_QUARTZ_SCHEDULER_SMTP_PORT: ((smtpPort))

## Project Layout

The project is composed of three directories.

- `admin` is the primary project directory and includes three sub directories

  - `Controller` - Handles REST api endpoints
  - `Jobs` - Includes all Quartz Jobs and Quartz Job Listeners
  - `Logger` - Contains code that helps an ILogger format JSON logs

- `CheckEngine` contains all code relating to the monitor plan evaluation job
  - `Database Access` - Configures a connection instance to the NpgSql database
  - `EcmpsCommon` - Defines common project variables
- `Quartz` is a cloned instance of [SilkierQuartz](https://github.com/maikebing/SilkierQuartz) that has been modified to fit the project's needs
  - `SilkierQuartz` - Clone of SilkierQuartz UI
  - `Quartz.Plugins.RecentHistory` - Copy of a plugin that SilkierQuartz uses to show recent job execution

## Creating a Quartz Job

All Jobs separate from the CheckEngine should be placed into `admin/Jobs`. Information about how to create and trigger a quartz job can be found [here](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/jobs-and-triggers.html#the-quartz-api).

After a job is created, to get the job registered and to ensure the job shows up in the Quartz UI, you must do the following:

- Inside of the ConfigureServices method within `admin/Startup.cs`, you must make a method call in the following format.
  - `JOBREFERENCE.RegisterWithQuartz(services)`
  - Example of the SendMail job: `SendMail.RegisterWithQuartz(services)`

Once a job has been registered, an instance of it can be created within the UI "Jobs" tab. Jobs can be triggered from within the UI "Triggers" tab.

## Logging

In order to handle JSON logging, A static LogHelper class has been put in place. In order to log information a call to `LogHelper.info()` or `LogHelper.error()` can be made.

- `LogVariable(string key, object value)` - Class that holds a key value pair to be JSON logged

- `LogHelper.info(ILogger logger, string Message, params LogVariable[] parameters)` - To use the info method pass the ILogger instance, a Message string, and an optional number of LogVariable() key-value objects
- `LogHelper.error(ILogger logger, string Message, params LogVariable[] parameters)` - behaves the same as info, but will generate and attach a unique error ID to the log

## License & Contributing

​
This project is licensed under the MIT License. We encourage you to read this project’s [License](https://github.com/US-EPA-CAMD/devops/blob/master/LICENSE), [Contributing Guidelines](https://github.com/US-EPA-CAMD/devops/blob/master/CONTRIBUTING.md), and [Code of Conduct](https://github.com/US-EPA-CAMD/devops/blob/master/CODE_OF_CONDUCT.md).

## Disclaimer

The United States Environmental Protection Agency (EPA) GitHub project code is provided on an "as is" basis and the user assumes responsibility for its use. EPA has relinquished control of the information and no longer has responsibility to protect the integrity , confidentiality, or availability of the information. Any reference to specific commercial products, processes, or services by service mark, trademark, manufacturer, or otherwise, does not constitute or imply their endorsement, recommendation or favoring by EPA. The EPA seal and logo shall not be used in any manner to imply endorsement of any commercial product or activity by EPA or the United States Government.
