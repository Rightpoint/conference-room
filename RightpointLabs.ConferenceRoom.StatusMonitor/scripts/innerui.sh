#!/bin/sh

xset s off
xset -dpms
xset s noblank

openbox --config-file ~pi/statusMonitor/scripts/openbox.xml &

# hide mouse cursor when idle
unclutter -idle 1 &

chromium-browser --user-data-dir=~pi/.config --app=http://rooms/ --no-sandbox
