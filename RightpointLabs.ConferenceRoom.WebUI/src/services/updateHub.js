(function() {
    'use strict';

    angular.module('app').factory('UpdateHub', function($rootScope, Hub, logger){
        var isFirstEvent = true;
        var hub = new Hub('UpdateHub', {
            listeners: {
                'Update': function (room) {
                    $rootScope.$apply(function() {
                        $rootScope.$broadcast('roomRefresh', room);
                    });
                }
            },
            errorHandler: function (error) {
                isFirstEvent = false;
                logger.error(error);
            },
            stateChanged: function(state) {
                switch(state.newState) {
                    case $.signalR.connectionState.connected:
                        if(!isFirstEvent) {
                            // no need to tell the user about the first event if it's a connection - we just want to tell them about future ones
                            logger.success('Live updates connected');
                        }
                        isFirstEvent = false;
                        break;
                    case $.signalR.connectionState.disconnected:
                        isFirstEvent = false;
                        logger.warning('Live updates suspended');
                        break;
                }
            }
        });

        return {};
    });
})();