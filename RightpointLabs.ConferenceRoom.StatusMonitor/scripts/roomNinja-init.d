# Provides:          roomNinja
# Required-Start:    $remote_fs $all
# Required-Stop:
# Default-Start:     2 3 4 5
# Default-Stop:
# Short-Description: Launch browser + background tool for Room Ninja
# Description:       Launch browser + background tool for Room Ninja
### END INIT INFO
~rooms/statusMonitor.sh &
sudo -u rooms ~rooms/ui.sh &
