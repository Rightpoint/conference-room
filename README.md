# Project structure

- .Net server application:
  - RightpointLabs.ConferenceRoom.Domain - domain layer
  - RightpointLabs.ConferenceRoom.Infrastructure - infrastructure
  - RightpointLabs.ConferenceRoom.Services - WebAPI services
- AngularJS+Bootstrap-based UI to show room availability - RightpointLabs.ConferenceRoom.WebUI
- Node.JS-based LED control based on availability: RightpointLabs.ConferenceRoom.StatusMonitor
- .Net Windows Service to control LEDs based on availability via serial port: RightpointLabs.ConferenceRoom.StatusMonitorService
  - Arduino app - RightpointLabs.ConferenceRoom.StatusMonitorService.ino

AppSettings.config and ConnectionStrings.config must be created in RightpointLabs.ConferenceRoom.Services directory (based on the .template files) in order to get that part to work.

Once the .Net app is running within Visual Studio (or IIS or whatever), make sure the proxy URL is right in the web.config (default is http://localhost:63915/api),
then run it with `gulp`.

If you get errors, check the server logs at http://localhost:63915/elmah.axd for clues.  