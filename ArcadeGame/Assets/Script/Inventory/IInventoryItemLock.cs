public interface IInventoryItemLock
{
    bool IsReserved { get; }
    bool IsReservedBy(object owner);
    
}