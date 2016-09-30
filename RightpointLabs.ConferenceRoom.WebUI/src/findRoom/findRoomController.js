(function() {
    'use strict;'

    angular.module('app').controller('FindRoomController', ['Restangular', '$state', '$timeout', '$q', '$modal', '$scope', 'statusService', 'logger', 'tokenService', '$stateParams', function(Restangular, $state, $timeout, $q, $modal, $scope, statusService, logger, tokenService, $stateParams) {
        var self = this;
        self.isLoading = true;

        tokenService.tokenInfo.then(function(tokenInfo) {
            if(!tokenInfo.controlledRooms || !tokenInfo.controlledRooms.length) {
                $state.go('home');
                return;
            }

            var roomAddress = tokenInfo.controlledRooms[0];

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
            };
            self.roomHasEquipment = function roomHasEquipment(room, equip) {
                return _.some(room.Equipment, function(ee) { return equip.text == ee; });
            };
            
            // Delegate the work to the server (fallback no longer needed)
            Restangular.all('room').one('all').one('status').get({ roomAddress: $stateParams.roomAddress || roomAddress }).then(function(data) {
                return data.map(function(i) {
                    return angular.merge({ Address: i.Address }, i.Info, i.Status);
                });
            }).then(function(allRooms) {
                allRooms.sort(function(a, b) { return a.DisplayName < b.DisplayName ? -1 : 1; });
                
                allRooms.forEach(function(r) {
                    r.Location = !r.BuildingName ? null : r.FloorName ? r.BuildingName + " - " + r.FloorName : r.BuildingName; 
                });
                
                var defaultRoom = _.findWhere(allRooms, { Address: roomAddress });
                if(defaultRoom) {
                    self.search.minSize = defaultRoom.Size || 0;
                    self.search.equipment = (defaultRoom.Equipment || []).map(function(i) {
                        return _.findWhere(self.equipmentChoices, { text: i });
                    });
                }

                self.locationChoices = _.unique(allRooms.map(function(r) { return r.Location; }).filter(function(l) { return l; })).map(function(l) { return { id: l, text: l }; }).concat([ { id: '', text: '<ANY>'} ]);
                self.sizeChoices = _.unique(allRooms.map(function(r) { return r.Size; }));
                self.sizeChoices.sort(function(a, b) { return a < b ? -1 : 1; });
                self.rooms = allRooms;
            }).then(function() {
                self.isLoading = false;
            }).catch(function(err) {
                logger.error(err);
                $state.go('home');
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
            
            function applyFilter() {
                self.searchResults = self.rooms.map(function(room) {
                    var matchLocation = !self.search.location || room.Location == self.search.location;
                    var matchSize = self.search.minSize <= (room.Size || 0);
                    var matchEquipment = _.all(self.search.equipment, function(e) {
                        return self.roomHasEquipment(room, e);
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
                self.scrollWidth = 30 + roomCount * (330 + 20*2) + groupCount * 60; // calculate the full width of the scrolling container based on the expected sizes of the various elements
            }
            
            $scope.$watch('[ c.rooms, c.search ]', applyFilter, true);
            $scope.$on('timeChanged', applyFilter);

            // we have a default room we're supposed to be managing - time out and go there after 60 seconds, but reset the timer on each action
            var redirectTimeout = null;
            function resetTimeout() {
                if(redirectTimeout) {
                $timeout.cancel(redirectTimeout);
                }
                redirectTimeout = $timeout(function() {
                    $state.go('home');
                }, 60000);
            }
            resetTimeout();
            var resetEvents = 'mousedown mouseover mouseout mousemove';
            angular.element(document).on(resetEvents, resetTimeout);
            $scope.$on('$destroy', function() {
                angular.element(document).off(resetEvents, resetTimeout);
            });
        });
        
    }]);
})();