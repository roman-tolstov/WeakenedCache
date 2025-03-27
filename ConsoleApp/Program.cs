using WeakenedCacheLib;

internal class Program
{
  private const string Key1 = "Key1";
  private const string Key2 = "Key2";
  private const string Key3 = "Key3";
  private const string Key4 = "Key4";
  private const string Key5 = "Key5";
  
  public static void Main(string[] args)
  {
    var cache = GetFilledCache();
    
    Console.WriteLine((cache.Take(Key1) as SampleRefType).Value);    // 1
    // or use generic
    Console.WriteLine(cache.Take<SampleRefType>(Key2)?.Value);       // 2 
    Console.WriteLine(cache.Take<SampleRefType>(Key3)?.Value);       // 3

    cache.WeakenUpTo(WeakenedCache.Priority.Normal);
    
    // you could also
    var someInstance = new SampleRefType();
    cache.Put(
      key: Key4,
      value: someInstance,
      priority: WeakenedCache.Priority.Normal,
      expiration: TimeSpan.FromSeconds(10),
      expirationType: WeakenedCache.ExpirationType.Weaken);
    
    // or
    var otherInstance = new SampleRefType();
    cache.Put(
      key: Key5,
      value: otherInstance,
      priority: WeakenedCache.Priority.Normal,
      expiration: TimeSpan.FromSeconds(10),
      expirationType: WeakenedCache.ExpirationType.Clear);
  }

  private static WeakenedCache GetFilledCache()
  {
    var wk = new WeakenedCache();
    
    var instance1 = new SampleRefType(1);
    var instance2 = new SampleRefType(2);
    var instance3 = new SampleRefType(3);
    
    wk.Put(Key1, instance1, WeakenedCache.Priority.Low);
    wk.Put(Key2, instance2, WeakenedCache.Priority.Normal);
    wk.Put(Key3, instance3, WeakenedCache.Priority.High);
    
    return wk;
  }
  
  private record SampleRefType(byte Value = 0);
}