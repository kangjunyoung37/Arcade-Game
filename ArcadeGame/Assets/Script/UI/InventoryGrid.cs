using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryGrid : MonoBehaviour
{
   [Header("Grid Settings")]
   public int gridWidth = 10;
   public int gridHeight = 10;

   public float cellSize = 50f;
   public float spacing = 0.5f;

   public Transform itemContainer;
   
   private float step => cellSize / spacing;

   private int[] gridMask;
   private ItemDrag[,] itemGrid;

   void Awake()
   {
      gridMask = new int[gridHeight];
      itemGrid = new ItemDrag[gridWidth, gridHeight];
   }

   public Vector2 GetPositionFromIndex(int x, int y)
   {
      float posX = x * step;
      float posY = -y * step;
      return new Vector2(posX, posY);
   }

   public Vector2Int GetIndexFromPosition(Vector2 localPosition)
   {
      int x = Mathf.FloorToInt(localPosition.x / step);
      int y = Mathf.FloorToInt(-localPosition.y / step);
      return new Vector2Int(x, y);
   }

   public bool CanPlaceItem(int startX, int startY, int width, int height)
   {
      if (startX < 0 || startY < 0 || startX + width > gridWidth || startY + height > gridHeight)
         return false;
      int itemRowMask = (1 << width) - 1;
      int shiftedMask = itemRowMask << startX;

      for (int y = 0; y < height; y++)
      {
         if ((gridMask[startY + y] & shiftedMask) != 0)
         {
            return false;
         }
      }
      return true;
   }
   public void AddItem(ItemDrag item, int startX, int startY)
   {
      int itemRowMask = ((1 << item.width) - 1) << startX;
      for (int y = 0; y < item.height; y++)
      {
         gridMask[startY + y] |= itemRowMask;
         for (int x = 0; x < item.width; x++)
         {
            itemGrid[startX + x, startY + y] = item;
         }
      }
   }
   
   public void RemoveItem(ItemDrag item, int startX, int startY)
   {
      int invertedMask = ~(((1 << item.width) - 1) << startX);
      for (int y = 0; y < item.height; y++)
      {
         gridMask[startY + y] &= invertedMask;
         for (int x = 0; x < item.width; x++)
         {
            itemGrid[startX + x, startY + y] = null;
         }
      }
   }
   
}
