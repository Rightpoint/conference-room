(function() {
    'use strict;'

    angular.module('app').controller('FindRoomController', ['Restangular', '$state', 'settings', '$timeout', '$q', '$modal', '$scope', 'statusService', function(Restangular, $state, settings, $timeout, $q, $modal, $scope, statusService) {
        if(!settings.defaultRoom) {
            $state.go('settings');
            return;
        }
        var self = this;
        self.isLoading = true;
        self.rooms = [];
        self.search = {
            minSize: 1,
            location: ''
        };
        self.searchResult = [];
        self.locationChoices = [ { id: '', text: '<ANY>'} ];
        Restangular.all('roomList').getList().then(function(roomLists) {
            return $q.all(roomLists.map(function(roomList) {
                return Restangular.one('roomList', roomList.Address).getList('rooms').then(function(rooms) {
                    if(rooms.filter(function(r) { return r.Address == settings.defaultRoom; }).length) {
                        // this is the right list - let's get our details
                        var allRooms = [];
                        return $q.all(rooms.map(function(room) {
                            return $q.all([
                                Restangular.one('room', room.Address).one('info').get({ securityKey: '' }),
                                Restangular.one('room', room.Address).one('status').get()
                            ]).then(function(calls){
                                allRooms.push(angular.merge({}, calls[0], calls[1]));
                            });
                        })).then(function() {
                            allRooms.sort(function(a, b) { return a.DisplayName < b.DisplayName ? -1 : 1; });
                            
                            allRooms.forEach(function(r) {
                                r.Location = !r.BuildingId ? null : r.Floor ? r.BuildingId + ": " + r.Floor : r.BuildingId; 
                            });
                            self.locationChoices = _.unique(allRooms.map(function(r) { return r.Location; }).filter(function(l) { return l; })).map(function(l) { return { id: l, text: l }; }).concat([ { id: '', text: '<ANY>'} ]);
                            
                            self.rooms = allRooms;
                        });
                    }
                    else {
                        // wrong list - do nothing
                    }
                });
            }));
        }).then(function() {
            self.isLoading = false;
        }).catch(function() {
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
        
        $scope.$watch('[ c.rooms, c.search ]', function() {
            self.searchResults = self.rooms.map(function(room) {
                var matchLocation = !self.search.location || room.Location == self.search.location;
                var matchSize = self.search.minSize <= room.Size;
                
                var s = statusService.status(room.CurrentMeeting, room.NearTermMeetings);
                var score = s.busy ? s.duration ? (1000 - Math.min(s.duration, 1000)) : 0 : s.duration ? 1000 + Math.min(s.duration, 1000) : 2000;
                
                room = angular.copy(room);
                if(matchSize && matchLocation) {
                    room.score = score;
                } else {
                    room.score = score - 2001;
                }
                
                // tag with some classes so we can do some styling
                room.class = {
                    'highlight-free': !s.busy,
                    'dim-not-match': !(matchSize && matchLocation)
                };
                return room;
            });
            self.searchResults.sort(function(a,b) {
                if(a.score != b.score) {
                    return a.score > b.score ? -1 : 1;
                }
                return a.DisplayName < b.DisplayName ? -1 : 1;
            });
        }, true);

        // we have a default room we're supposed to be managing - time out and go there after 60 seconds
        $timeout(function() {
            //$state.go('room');
        }, 60000);
    }]);
})();