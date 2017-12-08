## Xamarin Reactive Location - ALPHA


### Motivation

Location on each Xamarin platform is already event/push driven which is where Reactive models do great. This creates
a useful wrapper around the iOS/Android location APIs.


### Cross Platform-ish
- Location features/permissions change (see iOS 11 permission changes).
- Location features get fairly nuanced between platforms. This library will have a default setup that "works" 
but it has platform specific extensibility points for permissions, the location objects, and setting up start parameters.
Not having these things hidden gives you full control



### Thanks To
- https://github.com/jamesmontemagno/GeolocatorPlugin  for getting me started with location and inspiring parts of this library
