using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Tests")]
namespace WeakenedCacheLib;

public partial class WeakenedCache : IDisposable
{
  private const byte Priorities           = 3;
  private const byte CollectedCheckLimit  = 100;
  private const byte ExpirationCheckLimit = 100;
  
  private int _nextExpirableKeyToCheck;
  private int _nextWeakKeyToCheck;
  private bool _disposed;
  
  internal Dictionary<object, StrongEntry>[] StrongStorage { get; set; } = new Dictionary<object, StrongEntry>[Priorities];
  internal Dictionary<object, WeakEntry> WeakStorage       { get; set; } = new();
  internal List<object> ExpirableKeys                      { get; set; } = [];
  internal List<object> WeakKeys                           { get; set; } = [];

  public WeakenedCache()
  {
    for (var i = 0; i < Priorities; i++)
      StrongStorage[i] = new Dictionary<object, StrongEntry>();
  }

  public void Put<T>(object key, T value, Priority priority = Priority.Normal)
    where T : class
  {
    ClearCollected();
    ClearExpired(); 
    
    StrongStorage[(int)priority][key] = new StrongEntry(value, TimeSpan.Zero, ExpirationType.None);
  }

  public void Put<T>(object key, T value, Priority priority, TimeSpan expiration, ExpirationType expirationType)
    where T : class
  {
    ClearCollected();
    ClearExpired(); 
    
    StrongStorage[(int)priority][key] = new StrongEntry(value, expiration, expirationType);

    if (expirationType != ExpirationType.None)
      ExpirableKeys.Add(key);
  }

  public object? Take(object key)
  {
    ClearCollected();
    ClearExpired(); 
    
    for (int i = 0; i < Priorities; i++)
      if (StrongStorage[i].TryGetValue(key, out var strongEntry))
        return strongEntry.Value;

    if (WeakStorage.TryGetValue(key, out var weakEntry))
    {
      var value = weakEntry.Handle.Target;
      GC.KeepAlive(this);
      return value;
    }

    return null;
  }

  public T? Take<T>(object key)
    where T : class
  {
    return (T?)Take(key);
  }

  public void WeakenUpTo(Priority priority)
  {
    for (int i = 0; i <= (int)priority; i++)
    {
      foreach (var kvp in StrongStorage[i])
        AddWeakEntryFrom(kvp.Key, kvp.Value);

      StrongStorage[i].Clear();
    }
  }
  
  public void ClearExpired()
  {
    for (var i = 0; i < ExpirationCheckLimit && i < ExpirableKeys.Count; i++, _nextExpirableKeyToCheck++)
    {
      if (_nextExpirableKeyToCheck >= ExpirableKeys.Count)
        _nextExpirableKeyToCheck = 0;

      if (TryClearExpired(ExpirableKeys[_nextExpirableKeyToCheck]))
      {
        ExpirableKeys[_nextExpirableKeyToCheck] = ExpirableKeys[^1];
        ExpirableKeys.RemoveAt(ExpirableKeys.Count - 1);
        _nextExpirableKeyToCheck--;
      }
    }

    if (_nextExpirableKeyToCheck >= ExpirableKeys.Count)
      _nextExpirableKeyToCheck = 0;
  }

  public void ClearCollected()
  {
    for (var i = 0; i < CollectedCheckLimit && i < WeakKeys.Count; i++, _nextWeakKeyToCheck++)
    {
      if (_nextWeakKeyToCheck >= WeakKeys.Count)
        _nextWeakKeyToCheck = 0;

      if (TryClearCollected(WeakKeys[_nextWeakKeyToCheck]))
      {
        WeakKeys[_nextWeakKeyToCheck] = WeakKeys[^1];
        WeakKeys.RemoveAt(WeakKeys.Count - 1);
        _nextWeakKeyToCheck--;
      }
    }

    if (_nextWeakKeyToCheck >= WeakKeys.Count)
      _nextWeakKeyToCheck = 0;
  }

  private bool TryClearExpired(object key)
  {
    for (int i = 0; i < Priorities; i++)
    {
      if (StrongStorage[i].TryGetValue(key, out var strongEntry))
      {
        if (DateTime.Now - strongEntry.ExpirationDate > TimeSpan.Zero)
        {
          StrongStorage[i].Remove(key);
          if (strongEntry.ExpirationType == ExpirationType.Weaken) 
            AddWeakEntryFrom(key, strongEntry);
          
          return true;
        }
        
        return false;
      }
    }

    if (WeakStorage.TryGetValue(key, out var weakEntry))
    {
      if (DateTime.Now - weakEntry.ExpirationDate > TimeSpan.Zero)
      {
        if (weakEntry.ExpirationType == ExpirationType.Clear)
          WeakStorage.Remove(key);
       
        return true;
      }
      
      return false;
    }
    
    return true;
  }

  private bool TryClearCollected(object key)
  {
    if (WeakStorage.TryGetValue(key, out var weakEntry))
    {
      if (weakEntry.Handle.Target == null)
      {
        weakEntry.Handle.Free();
        WeakStorage.Remove(key);
        return true;
      }
      
      return false;
    }
    
    return true;
  }

  private void AddWeakEntryFrom(object key, StrongEntry strongEntry)
  {
    WeakKeys.Add(key);

    var handle = GCHandle.Alloc(strongEntry.Value, GCHandleType.Weak);
    WeakStorage[key] = new WeakEntry(handle, strongEntry.ExpirationDate, strongEntry.ExpirationType);
  }

  ~WeakenedCache()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
  }
  
  private void Dispose(bool disposing)
  {
    if (!_disposed)
    {
      foreach (var weakEntry in WeakStorage)
        weakEntry.Value.Handle.Free();
      
      if (disposing)
      {
        GC.SuppressFinalize(this);

        StrongStorage = null;
        WeakStorage = null;
        ExpirableKeys = null;
        WeakKeys = null;
      }
      
      _disposed = true;
    }
  }
}