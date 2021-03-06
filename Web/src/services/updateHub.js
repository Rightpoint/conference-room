(function() {
    'use strict';

    angular.module('app').factory('UpdateHub', ['$rootScope', 'Hub', 'logger', 'authToken', function($rootScope, Hub, logger, authToken){
        var isFirstEvent = true;
        var hub = new Hub('UpdateHub', {
            listeners: {
                'Update': function (room) {
                    $rootScope.$apply(function() {
                        $rootScope.$broadcast('roomRefresh', room);
                    });
                },
                'DeviceChanged': function (device) {
                    $rootScope.$apply(function() {
                        $rootScope.$broadcast('deviceChanged', device);
                    });
                },
                'RefreshAll': function() {
                    window.location.reload();
                }
            },
            methods: [ 'clientActive', 'authenticate' ],
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

                        // let the server know who we are
                        console.log('calling authenticate with ', authToken);
                        hub.authenticate(authToken);

                        break;
                    case $.signalR.connectionState.disconnected:
                        isFirstEvent = false;
                        logger.warning('Live updates suspended');
                        break;
                }
            }
        });

        return hub;
    }]);
})();