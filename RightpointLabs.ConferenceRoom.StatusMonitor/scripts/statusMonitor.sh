#/bin/sh

sleep 15 # we're starting too early

while true
do
        /usr/bin/node ~rooms/conference-room/RightpointLabs.ConferenceRoom.StatusMonitor/index.js 2>&1 1>>~rooms/statusMonitor.log
        sleep 1
done
