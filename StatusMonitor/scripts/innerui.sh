#!/bin/sh

xset s off
xset -dpms
xset s noblank

openbox --config-file ~pi/statusMonitor/scripts/openbox.xml &

# hide mouse cursor when idle
unclutter -idle 1 &

while -t ~pi/statusMonitor/deviceKey
do
    echo "waiting for device key"
    sleep 3
done

APP=http://rooms.labs.rightpoint.com/#`~pi/statusMonitor/deviceKey`

chromium-browser --user-data-dir=/tmp/.config --app=$APP --no-sandbox --disable-pinch --touch-events --kiosk
