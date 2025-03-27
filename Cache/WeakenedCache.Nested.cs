using System.Reflection;
using System.Runtime.InteropServices;

namespace WeakenedCacheLib;

public partial class WeakenedCache
{
  internal struct StrongEntry(object value, TimeSpan expiration, ExpirationType expirationType)
  {
    public object Value                  = value;
    public DateTime ExpirationDate       = DateTime.Now.Add(expiration);
    public ExpirationType ExpirationType = expirationType;
  }

  internal record struct WeakEntry(GCHandle Handle, DateTime ExpirationDate, ExpirationType ExpirationType);

  public enum Priority : byte
  {
    Low    = 0,
    Normal = 1,
    High   = 2,
  }

  public enum ExpirationType : byte
  {
    None   = 0,
    Clear  = 1,
    Weaken = 2,
  }
}