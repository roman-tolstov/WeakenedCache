using WeakenedCacheLib;
using Xunit;

public class WeakenedCacheTests
{
  private readonly object _key1 = "key1";
  private readonly object _key2 = "key2";
  private readonly object _key3 = "key3";
  
  [Fact]
  public void PutItemIntoCache_CanTakeIt()
  {
    // Arrange
    var wk = new WeakenedCache();
    var someInstance = new SomeType();

    // Act
    wk.Put(_key1, someInstance);

    // Assert
    Assert.NotNull(wk.Take(_key1));
  }
  
  [Fact]
  public void PutItemIntoCache_FailWhenTryToTakeItAsOtherType()
  {
    // Arrange
    var wk = new WeakenedCache();
    var someInstance = new SomeType();

    // Act
    wk.Put(_key1, someInstance);

    // Assert
    Assert.Throws<InvalidCastException>(() => { wk.Take<OtherType>(_key1); });
  }

  [Fact]
  public void PutItemWithWeakenExpiration_AfterTriggerBySomeAction_ItGoesToWeakStorage()
  {
    // Arrange
    var wk = new WeakenedCache();
    var someInstance = new SomeType();
    var otherInstance = new OtherType();

    wk.Put(_key1,
      someInstance,
      WeakenedCache.Priority.Normal,
      TimeSpan.Zero,
      WeakenedCache.ExpirationType.Weaken);
    
    // Act
    wk.Put(_key2, otherInstance, WeakenedCache.Priority.Low);
    
    // Assert
    Assert.Contains(wk.WeakStorage, x => x.Key == _key1 && ReferenceEquals(x.Value.Handle.Target, someInstance));
    Assert.DoesNotContain(wk.ExpirableKeys, k => k == _key1);
    Assert.DoesNotContain(
      wk.StrongStorage.SelectMany(x => x),
      pair => pair.Key == _key1 || ReferenceEquals(pair.Value.Value, someInstance));
  }
  
  [Fact]
  public void PutItemWithClearExpiration_AfterTriggerBySomeAction_StoragesDontHaveIt()
  {
    // Arrange
    var wk = new WeakenedCache();
    var someInstance = new SomeType();
    var otherInstance = new OtherType();
  
    wk.Put(_key1,
      someInstance,
      WeakenedCache.Priority.Normal,
      TimeSpan.Zero,
      WeakenedCache.ExpirationType.Clear);
    
    // Act
    wk.Put(_key2, otherInstance, WeakenedCache.Priority.Low);
    
    // Assert
    Assert.DoesNotContain(wk.WeakStorage, x => x.Key == _key1 || ReferenceEquals(x.Value.Handle.Target, someInstance));
    Assert.DoesNotContain(wk.ExpirableKeys, k => k == _key1); // ???
    Assert.DoesNotContain(
      wk.StrongStorage.SelectMany(x => x), 
      pair => pair.Key == _key1 || ReferenceEquals(pair.Value.Value, someInstance));
  }

  [Fact]
  public void WeakenUpToNormalPriority_OnlyHighPriorityStaysInStrongStorage()
  {
    // Arrange
    var wk = new WeakenedCache();
    var firstInstance = new SomeType();
    var secondInstance = new SomeType();
    var thirdInstance = new SomeType();
    
    wk.Put(_key1, firstInstance, WeakenedCache.Priority.Low);
    wk.Put(_key2, secondInstance, WeakenedCache.Priority.Normal);
    wk.Put(_key3, thirdInstance, WeakenedCache.Priority.High);
    
    // Act
    wk.WeakenUpTo(WeakenedCache.Priority.Normal); 
    
    // Assert
    Assert.Contains(
      wk.StrongStorage[(int)WeakenedCache.Priority.High],
      kvp => kvp.Key == _key3 && ReferenceEquals(kvp.Value.Value, thirdInstance));
    Assert.DoesNotContain(
      wk.StrongStorage[(int)WeakenedCache.Priority.Normal],
      kvp => kvp.Key == _key2 || ReferenceEquals(kvp.Value.Value, secondInstance));
    Assert.DoesNotContain(
      wk.StrongStorage[(int)WeakenedCache.Priority.Low],
      kvp => kvp.Key == _key1 || ReferenceEquals(kvp.Value.Value, firstInstance));
  }
}

internal record SomeType;
internal record OtherType;