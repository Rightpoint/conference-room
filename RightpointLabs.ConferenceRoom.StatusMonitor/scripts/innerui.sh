#!/bin/sh

xset s off
xset -dpms
xset s noblank

# load calibration so that we don't have flipped coordinates :)
. /etc/pointercal.xinput

openbox --config-file ~rooms/.openbox.xml &
epiphany-browser -a --profile ~rooms/.config http://rooms.labs.rightpoint.com/

