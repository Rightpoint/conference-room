# Project structure

- .Net server application:
  - RightpointLabs.ConferenceRoom.Domain - domain layer
  - RightpointLabs.ConferenceRoom.Infrastructure - infrastructure
  - RightpointLabs.ConferenceRoom.Services - WebAPI services
- AngularJS+Bootstrap-based UI to show room availability - RightpointLabs.ConferenceRoom.WebUI
- Node.JS-based LED control based on availability: RightpointLabs.ConferenceRoom.StatusMonitor
- .Net Windows Service to control LEDs based on availability via serial port: RightpointLabs.ConferenceRoom.StatusMonitorService
  - Arduino app - RightpointLabs.ConferenceRoom.StatusMonitorService.ino
