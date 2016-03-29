(function() {
    'use strict;'

    angular.module('app').controller('FindRoomController', ['Restangular', '$state', 'settings', '$timeout', '$q', '$modal', '$scope', 'statusService', 'logger', function(Restangular, $state, settings, $timeout, $q, $modal, $scope, statusService, logger) {
        if(!settings.defaultRoom) {
            $state.go('settings');
            return;
        }
        var self = this;
        self.isLoading = true;
        self.rooms = [];
        self.search = {
            minSize: 1,
            location: '',
            equipment: []
        };
        self.sizeChoices = [ 1, 2, 3, 4, 6, 8, 10, 12, 40 ];
        self.hasBiggerRoom = function hasBiggerRoom() {
            return !!self.sizeChoices.filter(function(c) { return c > self.search.minSize; }).length;
        };
        self.biggerRoom = function biggerRoom() {
            self.search.minSize = self.sizeChoices.filter(function(c) { return c > self.search.minSize; })[0] || _.last(self.sizeChoices);
        };
        self.smallerRoom = function smallerRoom() {
            self.search.minSize = _.last(self.sizeChoices.filter(function(c) { return c < self.search.minSize; })) || self.sizeChoices[0];
        };
        self.searchResult = [];
        self.locationChoices = [ { id: '', text: '<ANY>'} ];
        function selfMatch (item, room) {
            return _.some(room.Equipment, item);
        }
        self.equipmentChoices = [ 
            { text: 'Display', icon: 'custom-icon-display'},
            { text: 'Telephone', icon: 'custom-icon-phone', displayText: 'Phone' },
            { text: 'Whiteboard', icon: 'custom-icon-whiteboard' }
        ];
        self.equipmentSelected = function(e) {
            return _.some(self.search.equipment, function(i) { return i === e; });
        };
        self.toggleEquipment = function(e) {
            if(self.equipmentSelected(e)) {
                self.search.equipment = self.search.equipment.filter(function(i) { return i != e; }); 
            } else {
                self.search.equipment = self.search.equipment.concat([e]);
            }
        };
        self.floor = function(e) {
            return Math.floor(e);
        }
        
        // try to delegate the work to the server.  If that doesn't work, then we'll fallback to calling the normal APIs
        Restangular.all('room').one('all').getList('status', { roomAddress: settings.defaultRoom }).then(function(data) {
            return data.map(function(i) {
                return angular.merge({ Address: i.Address }, i.Info, i.Status);
            });
        }).catch(function() {
            // fallback
            return Restangular.all('roomList').getList().then(function(roomLists) {
                var allRooms = [];
                return $q.all(roomLists.map(function(roomList) {
                    return Restangular.one('roomList', roomList.Address).getList('rooms').then(function(rooms) {
                        return $q.all(rooms.map(function(room) {
                            return $q.all([
                                Restangular.one('room', room.Address).one('info').get({ securityKey: '' }),
                                Restangular.one('room', room.Address).one('status').get()
                            ]).then(function(calls){
                                var merged = angular.merge({ Address: room.Address }, calls[0], calls[1]);
                                allRooms.push(merged);
                            });
                        }));
                    });
                })).then(function() {
                    return allRooms;
                });
            });
        }).then(function(allRooms) {
            allRooms.sort(function(a, b) { return a.DisplayName < b.DisplayName ? -1 : 1; });
            
            allRooms.forEach(function(r) {
                r.Location = !r.BuildingId ? null : r.Floor ? r.BuildingId + " - " + r.Floor + 'th floor' : r.BuildingId; 
            });
            
            var defaultRoom = _.findWhere(allRooms, { Address: settings.defaultRoom });
            if(defaultRoom) {
                self.search.minSize = defaultRoom.Size || 0;
                self.search.equipment = (defaultRoom.Equipment || []).map(function(i) {
                    return _.findWhere(self.equipmentChoices, { text: i });
                });
                if(defaultRoom.BuildingId) {
                    // default room has a building, so let's filter our data by that (server may have done it already, but that's ok)
                    allRooms = _.where(allRooms, { BuildingId: defaultRoom.BuildingId });
                }
            }

            self.locationChoices = _.unique(allRooms.map(function(r) { return r.Location; }).filter(function(l) { return l; })).map(function(l) { return { id: l, text: l }; }).concat([ { id: '', text: '<ANY>'} ]);
            self.sizeChoices = _.unique(allRooms.map(function(r) { return r.Size; }));
            self.sizeChoices.sort(function(a, b) { return a < b ? -1 : 1; });
            self.rooms = allRooms;
        }).then(function() {
            self.isLoading = false;
        }).catch(function(err) {
            logger.error(err);
            $state.go('settings');
        });
        
        self.showDetails = function showDetails(meeting) {
            $modal.open({
                templateUrl: 'meeting/meeting.html',
                controller: 'MeetingController',
                controllerAs: 'c',
                resolve: {
                    meeting: function() {
                        return meeting;
                    }
                }
            });
        };
        
        self.showRoomDetails = function showDetails(calendar) {
            var room = _.find(self.rooms, { DisplayName: calendar.DisplayName });
            $modal.open({
                templateUrl: 'roomDetail/roomDetail.html',
                controller: 'RoomDetailController',
                controllerAs: 'c',
                resolve: {
                    room: function() {
                        return room;
                    }
                }
            });
        };
        
        $scope.$watch('[ c.rooms, c.search ]', function() {
            self.searchResults = self.rooms.map(function(room) {
                var matchLocation = !self.search.location || room.Location == self.search.location;
                var matchSize = self.search.minSize <= (room.Size || 0);
                var matchEquipment = _.all(self.search.equipment, function(e) {
                    return _.some(room.Equipment, function(ee) { return e.text == ee; });
                });
                
                room = angular.copy(room);
                room.match = matchLocation && matchSize && matchEquipment;
                room.status = statusService.status(room.CurrentMeeting, room.NearTermMeetings);
                room.status.freeFor = room.status.freeFor === 0 ? 0 : Math.floor(Math.min(120, room.status.freeFor || 120)); 
                room.group = room.status.freeAt === 0 ? 'Available Now' : room.status.freeAt ? ('Available at ' + moment(room.status.freeAt).format('h:mm a')) : null;
                return room;
            }).filter(function(r) {
                return r.match && r.group && r.status.freeFor > 0;
            });
            var roomCount = self.searchResults.length;
            self.searchResults = _.pairs(_.groupBy(self.searchResults, 'group')).map(function(g) {
                return {
                    key: g[0],
                    rooms: g[1],
                    freeAt: g[1][0].status.freeAt
                };
            });;
            self.searchResults.sort(function(a,b) {
                if(a.freeAt === 0) {
                    return -1;
                }
                if(b.freeAt === 0) {
                    return 1;
                }
                if(!a.freeAt) { 
                    return 1;
                }
                if(!b.freeAt) {
                    return -1;
                }
                if(a.freeAt != b.freeAt) {
                    return a.freeAt < b.freeAt ? -1 : 1;
                }
                return a.key < b.key ? -1 : 1;
            });
            self.searchResults.forEach(function (g) {
                g.rooms.sort(function(a,b) {
                    if(a.status.freeFor != b.status.freeFor) {
                        return a.status.freeFor > b.status.freeFor ? -1 : 1;
                    }
                    return a.DisplayText < b.DisplayText ? -1 : 1;
                });
            });
            var groupCount = self.searchResults.length;
            self.scrollWidth = 30 + roomCount * (330 + 20*2) + groupCount * 20; // calculate the full width of the scrolling container based on the expected sizes of the various elements
        }, true);

        // we have a default room we're supposed to be managing - time out and go there after 60 seconds
        $timeout(function() {
            //$state.go('room');
        }, 60000);
    }]);
})();