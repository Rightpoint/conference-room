(function() {
    'use strict';

    angular.module('app').factory('UpdateHub', function($rootScope, Hub, logger){
        var hub = new Hub('UpdateHub', {
            listeners: {
                'Update': function (room) {
                    $rootScope.$apply(function() {
                        $rootScope.$broadcast('roomRefresh', room);
                    });
                }
            },
            errorHandler: function (error) {
                logger.error(error);
            },
            stateChanged: function(state) {
                switch(state.newState) {
                    case $.signalR.connectionState.connected:
                        logger.success('Live updates connected');
                        break;
                    case $.signalR.connectionState.disconnected:
                        logger.warn('Live updates suspended');
                        break;
                }
            }
        });

        return {};
    });
})();