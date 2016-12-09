# Project structure

- .Net server application:
  - Domain - domain layer
  - Infrastructure - infrastructure
  - Services - WebAPI services
    - src - AngularJS+Bootstrap-based UI for devices and normal users
- StatusMonitor - Node.JS-based LED control based on availability

This application now is x64-only on the .Net server-side, and requires the installation of the UCMA 4.0 SDK (https://www.microsoft.com/en-us/download/details.aspx?id=35463) to build.

AppSettings.config and ConnectionStrings.config must be created in Web directory (based on the .template files) in order to get that part to work.

Once the .Net app is running within Visual Studio (or IIS or whatever), make sure the proxy URL is right in the web.config (default is http://localhost:63915/api),
then run the front-end with `gulp` (from the Web directory)

If you get errors, check the server logs at http://localhost:63915/elmah.axd for clues.  

# Getting started:

  1. In Web project, copy AppSettings.template.config and ConnectionStrings.Template.config to AppSettings.config and ConnectionStrings.config.
  2. Create an Azure AD application and register in AppSettings.  Details TODO
  3. Configure ConnectionStrings + AppSetttings for Mongo (install if needed).  TODO
  4. Go to /admin/home - you should be prompted to log in and then get a "403.0 - Not an Admin" error from IIS.  Make sure your username is displayed properly - you'll need it later.
  5. Connect to the mongo DB you indicated - a globaladministrators collection should have been created with a record for the username "REPLACE THIS WITH YOUR USERNAME".  Edit (or replace) the record to put your username in from the previous step.
  6. Using the admin section (/admin), create an organization, building, floor, and room.
  7. Manually add a record to the organizationserviceconfiguration collection in Mongo that includes your Organization ID and supplies the parameters to connect to Exchange (UI is TODO).  It should look kinda like this (you may need to disable impersonation and change notification if using a non-admin account):

		{
			"_id" : ObjectId("....."),
			"OrganizationId" : ObjectId("....."),
			"ServiceName" : "Exchange",
			"Parameters" : {
				"Username" : ".....",
				"Password" : "......",
				"ServiceUrl" : "https://outlook.office365.com/EWS/Exchange.asmx",
				"IgnoreFree" : false,
				"UseChangeNotification" : true,
				"ImpersonateForAllCalls" : true,
				"EmailDomains" : [ 
					"......"
				]
			}
		}

  8. Set a joinKey on your organization in Mongo (UI is TODO)
  9. Copy the StatusMonitor project to a Raspberry Pi and update config.json (organization ID and joinKey are critical - rest are local config options).
  10. Install the startup scripts for the Pi and launch things.
  11. Use the Admin UI to assign a room to the device - the device should automatically navigate to the screen showing that room when you do.
  12. After confirming the Pi setup is good, remove the deviceKey file, shut it down, and clone the SD card for reuse.
  13. After a new device comes online, it will use the organizationId and joinKey to create a new device, which you just need to map to a room via the admin UI and you're in business.