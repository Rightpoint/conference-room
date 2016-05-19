# Project structure

- .Net server application:
  - RightpointLabs.ConferenceRoom.Domain - domain layer
  - RightpointLabs.ConferenceRoom.Infrastructure - infrastructure
  - RightpointLabs.ConferenceRoom.Services - WebAPI services
- AngularJS+Bootstrap-based UI to show room availability - RightpointLabs.ConferenceRoom.WebUI
- Node.JS-based LED control based on availability: RightpointLabs.ConferenceRoom.StatusMonitor

This application now is x64-only on the .Net server-side, and requires the installation of the UCMA 4.0 SDK (https://www.microsoft.com/en-us/download/details.aspx?id=35463) to build.

AppSettings.config and ConnectionStrings.config must be created in RightpointLabs.ConferenceRoom.Services directory (based on the .template files) in order to get that part to work.

Once the .Net app is running within Visual Studio (or IIS or whatever), make sure the proxy URL is right in the web.config (default is http://localhost:63915/api),
then run it with `gulp`.

If you get errors, check the server logs at http://localhost:63915/elmah.axd for clues.  

To use this on a tablet with a windows service to control the lights, take a look at the windows-tablet branch.
