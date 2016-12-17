#!/bin/sh

xset s off
xset -dpms
xset s noblank

openbox --config-file ~pi/statusMonitor/scripts/openbox.xml &

# hide mouse cursor when idle
unclutter -idle 1 &

while [ ! -f ~pi/statusMonitor/devicekey ]
do
    echo "waiting for device key"
    sleep 3
done

APP=https://rprooms.azurewebsites.net/#`cat ~pi/statusMonitor/devicekey`

chromium-browser --user-data-dir=/tmp/.config --app=$APP --no-sandbox --disable-pinch --touch-events --kiosk
