# WeakenedCache
Acts like a regular cache but with the ability to make its entries weakly referenced   
## Create cache
``` csharp
var cache = new WeakenedCache();
```
## Fill it
``` csharp
var instance1 = new SampleRefType(1);
var instance2 = new SampleRefType(2);
var instance3 = new SampleRefType(3);

cache.Put(Key1, instance1, WeakenedCache.Priority.Low);        
cache.Put(Key2, instance2, WeakenedCache.Priority.Normal);
cache.Put(Key3, instance3, WeakenedCache.Priority.High);
```
## Get values
``` csharp
Console.WriteLine((cache.Take(Key1) as SampleRefType).Value);    // 1
``` 
## or use generic
``` csharp
Console.WriteLine(cache.Take<SampleRefType>(Key2)?.Value);       // 2 
Console.WriteLine(cache.Take<SampleRefType>(Key3)?.Value);       // 3
```
## Weaken items of some priorities
``` csharp
cache.WeakenUpTo(WeakenedCache.Priority.Normal);
```

## Put items with expiration of Weaken type
``` csharp
var someInstance = new SampleRefType();

cache.Put(
  key: Key4,
  value: someInstance,
  priority: WeakenedCache.Priority.Normal,
  expiration: TimeSpan.FromSeconds(10),
  expirationType: WeakenedCache.ExpirationType.Weaken);
```
## Or Clear type
``` csharp
var otherInstance = new SampleRefType();

cache.Put(
    key: Key5,
    value: otherInstance,
    priority: WeakenedCache.Priority.Normal,
    expiration: TimeSpan.FromSeconds(10),
    expirationType: WeakenedCache.ExpirationType.Clear);
```
