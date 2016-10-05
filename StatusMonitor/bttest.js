var beacon = require('eddystone-beacon');
var options = {
	name: 'Beacon',
	txPowerLevel: -22,
	tlmCount: 2,
	tlmPeriod: 10
};
//beacon.advertiseUrl('http://rightpoint.com/room', options);
var namespace = '5269676874706f696e74'; // string.Join("","Rightpoint".Select(i => ((int)i).ToString("x2")))
var uid = '73756e726f6f'; // string.Join("","sunroo".Select(i => ((int)i).ToString("x2")))
beacon.advertiseUid(namespace, uid, options);
