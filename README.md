# `Easey-Quartz-Scheduler`

[![GitHub](https://img.shields.io/github/license/US-EPA-CAMD/easey-ecmps-ui)](https://github.com/US-EPA-CAMD/easey-ecmps-ui/blob/develop/LICENSE)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=US-EPA-CAMD_easey-quartz-scheduler&metric=alert_status)](https://sonarcloud.io/dashboard?id=US-EPA-CAMD_easey-quartz-scheduler)
[![Develop Branch Pipeline](https://github.com/US-EPA-CAMD/easey-quartz-scheduler/workflows/Develop%20Branch%20Workflow/badge.svg)](https://github.com/US-EPA-CAMD/easey-ecmps-ui/actions)<br>
Quartz Job Scheduler

## `Available Scripts`

From within the `admin` folder within the project directory, you can run:

### `yarn start`

Runs the app in the development mode.<br />

- Open [http://localhost:5000/quartz](http://localhost:5000/quartz) to view Home page.

You will also see any lint errors in the console.

### `yarn build`

Builds the app for production to the `dist` folder.<br />

## Environment Variables

Environment variables need to be set in order to run the application properly. Most environment variables have default values, but EASEY_AUTH_API, EASEY_QUARTZ_SCHEDULER_EMAIL, EASEY_QUARTZ_SCHEDULER_SMTP_HOST, and EASEY_QUARTZ_SCHEDULER_SMTP_PORT need to be manually set. 

REST API URL's required by the application...

- EASEY_AUTH_API: https://((apiHost))/api/auth-mgmt

Other application environment variables:

- EASEY_QUARTZ_SCHEDULER_TITLE: ((title))  
- EASEY_QUARTZ_SCHEDULER_HOST: ((host))  
- EASEY_QUARTZ_SCHEDULER_PATH: ((path))  
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
