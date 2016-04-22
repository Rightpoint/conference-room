#!/bin/sh

sleep 3 # we're starting too early

while true
do
        /usr/bin/node ~pi/statusMonitor/index.js 2>&1 1>>~pi/statusMonitor.log
        sleep 1
done
