#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

internal class BlockMatrixBlock<T>
{
    protected readonly Vector2 Pos;
    protected readonly Vector2 Size;


    protected bool IsEmpty;
    
    protected readonly T? DefaultValue;


    protected BlockMatrixBlock(T? defaultValue, Vector2 pos, Vector2 size)
    {
        DefaultValue = defaultValue;
        Pos = pos;
        Size = size;
    }

    public virtual bool Add(Vector2 targetPos, T? value, Vector2 absolutePos = default)
    {
        return false;
    }
    
    public virtual bool Remove(Vector2 pos)
    {
        return false;
    }
    
    internal virtual T? Get(Vector2 targetPos = default)
    {
        return default;
    }
    
    public virtual T? this[int x, int y]
    {
        get => Get(new Vector2(x, y));
        set => Add(new Vector2(x, y), value);
    }

}


internal class BlockMatrix<T> : BlockMatrixBlock<T>
{
    /// <summary>
    /// 2D Array containing all of the sub-blocks
    /// </summary>
    private readonly BlockMatrixBlock<T>?[,] _blocks;

    /// <summary>
    /// size of one of the contained blocks
    /// </summary>
    private readonly Vector2 _blockSize;

    private readonly Vector2 _blockCount;

    
    
    public BlockMatrix(T? defaultValue, Vector2 size, Vector2 pos = new()) : base(defaultValue, pos, size)
    {
        // calculate largest W/H factors
        var wLargestFactor = BlockMatrixUtil.LargestFactor((int)size.X);
        var hLargestFactor = BlockMatrixUtil.LargestFactor((int)size.Y);
        
        
        // instantiate _blockSize to be as large as possible, but smaller than the matrix, while keeping the matrix size divisible by it
        _blockSize = new Vector2(wLargestFactor, hLargestFactor);
        
        
        // instantiate _blocks to be of size: smallest non-1 (unless matrix size is prime) factor of passed size.
        // also store this value in a field for later use
        _blockCount = new Vector2((int)(size.X / wLargestFactor), (int)(size.X / hLargestFactor));
        _blocks = new BlockMatrixBlock<T>[(int)_blockCount.X, (int)_blockCount.X];

    }


    /// <summary>
    /// Gets a value from the matrix.
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vector2 targetPos = default)
    {
        // get the pos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, Size);

        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _blockCount.X || blockPos.Y < 0 || blockPos.Y > _blockCount.Y)
            throw new Exception("Index out of Bounds");
        
        // get the next block
        var nextBlock = _blocks[(int)blockPos.X, (int)blockPos.Y];
        
        // calculate the new targetPos
        var blockSign = new Vector2(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _blockSize / _blockCount;
        var newTargetPos = targetPos - (nextBlockBlockSize * blockSign);

        return nextBlock == null ? DefaultValue : nextBlock.Get(newTargetPos);
    }

    public override T? this[int x, int y]
    {
        get => Get(new Vector2(x, y));
        set => Add(new Vector2(x, y), value, new Vector2(x, y));
    }
    
    public override bool Add(Vector2 targetPos, T? value, Vector2 absolutePos = default)
    {
        if (value == null)
            return Remove(targetPos);

        if (value.Equals(DefaultValue))
            return Remove(targetPos);
        
        // get the pos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, Size);
        
        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _blockCount.X || blockPos.Y < 0 || blockPos.Y > _blockCount.Y)
            throw new Exception("Index out of Bounds");

        // get the next block
        var nextBlock = _blocks[(int)blockPos.X, (int)blockPos.Y];
        
        // if part is null, create it
        if (nextBlock == null)
        {
            nextBlock = 
                _blockSize == Vector2.One ?
                    new BlockMatrixValue<T>(DefaultValue, absolutePos, targetPos, Vector2.One, value) :
                    new BlockMatrix<T>(DefaultValue, _blockSize, targetPos);

            _blocks[(int)blockPos.X, (int)blockPos.Y] = nextBlock;
        }

        // calculate the new targetPos
        var blockSign = new Vector2(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _blockSize / _blockCount;
        var newTargetPos = targetPos - (nextBlockBlockSize * blockSign);
        
        // recursively add the value
        var success = nextBlock.Add(newTargetPos, value, absolutePos);

        // if we added something, the matrix will no longer be empty
        if (success)
            IsEmpty = false;
        
        return success;
    }
    
    /// <summary>
    /// Removes a value in the matrix.
    /// </summary>
    /// <param name="pos">the position to remove the value at</param>
    /// <returns>a bool describing if the matrix was changed</returns>
    public override bool Remove(Vector2 pos)
    {
        var success = false;
        
        var contentPos = BlockMatrixUtil.GetBlockIndexFromPos(pos - Pos, _blockSize);

        var part = _blocks[(int)contentPos.X, (int)contentPos.Y];

        // if part does not exist to begin with, nothing will change
        if (part == null) return false;
        
        
        // if part refers to a BlockMatrixContainer, pass the call downwards.
        if (part.GetType() == typeof(BlockMatrix<T>))
        {
            success = part.Remove(pos);
        }
        
        // otherwise, if targetPos refers to a BlockMatrixValue, remove it and flatten the matrix.
        else if (part.GetType() == typeof(BlockMatrixValue<T>))
        {
            //TODO: compression/removal
            
            success = true;
        }

        // update the IsEmpty flag if something changed
        if (success)
            IsEmpty = CheckEmpty();
        
        return success;
    }

    /// <summary>
    /// Converts the BlockMatrix into an array, passing the default value if none is present
    /// </summary>
    public T?[,] ToArray()
    {
        var array = new T?[(int)Size.X, (int)Size.Y];

        var half = Size / 2f;
        
        for (var x = 0; x < (int)Size.X; x++)
        {
            for (var y = 0; y < (int)Size.Y; y++)
            {
                array[x, y] = Get(new Vector2(x - half.X, y - half.Y));
            }
        }

        return array;
    }
    
    
    /// <summary>
    /// Converts the BlockMatrix to a list of set (not-default-value) elements
    /// </summary>
    public List<T?> ToList()
    {
        var list = new List<T?>();

        foreach (var block in _blocks)
        {
            switch (block)
            {
                case null:
                    continue;
                
                case BlockMatrix<T> blockMatrix:
                    list.AddRange(blockMatrix.ToList());
                    break;
                case BlockMatrixValue<T> blockValue:
                    list.Add(blockValue.Get());
                    break;
            }
        }
        
        return list;
    }
    
    /// <summary>
    /// Converts the BlockMatrix to a dictionary of type position:element containing all set (not-default-value) elements
    /// </summary>
    public Dictionary<Vector2, T?> ToListWithPos()
    {
        var list = new Dictionary<Vector2, T?>();

        foreach (var block in _blocks)
        {
            switch (block)
            {
                case null:
                    continue;
                
                case BlockMatrix<T> blockMatrix:
                    foreach (var element in blockMatrix.ToListWithPos())
                        list.Add(element.Key, element.Value);
                    break;
                
                case BlockMatrixValue<T> blockValue:
                    list.Add(blockValue.GetPos(), blockValue.Get());
                    break;
            }
        }
        
        return list;
    }


    private bool CheckEmpty()
    {
        var isEmpty = true;
        
        foreach (var part in _blocks)
        {
            // ignore null (therefore empty) parts
            if (part == null) continue;
            
            // if it is a BlockMatrixContainer, pass the call downwards
            if (part.GetType() == typeof(BlockMatrix<T>))
            {
                isEmpty &= ((BlockMatrix<T>)part).IsEmpty;
            }
            // otherwise, if it is a BlockMatrixValue, return false as the matrix is not empty
            else if (part.GetType() == typeof(BlockMatrixValue<T>))
            {
                return false;
            }
        }

        return isEmpty;
    }
    
    
}

internal class BlockMatrixValue<T> : BlockMatrixBlock<T>
{
    private T _value;
    private readonly Vector2 _absolutePos;
    
    public BlockMatrixValue(T? defaultValue, Vector2 absolutePos, Vector2 pos, Vector2 size, T value) : base(defaultValue, pos, size)
    {
        _absolutePos = absolutePos;
        _value = value;
    }

    public override bool Add(Vector2 targetPos, T? value, Vector2 absolutePos = default)
    {
        if (value == null)
            return false;
        
        _value = value;
        return true;
    }

    internal override T Get(Vector2 targetPos = default)
    {
        return _value;
    }
    
    public Vector2 GetPos()
    {
        return _absolutePos;
    }
}

internal static class BlockMatrixUtil
{
    internal static Vector2 GetBlockIndexFromPos(Vector2 localPos, Vector2 blockSize)
    {
        var contentPos = new Vector2(
            (int)Math.Floor(localPos.X / blockSize.X) + 1,
            (int)Math.Floor(localPos.Y / blockSize.Y) + 1
        );
        
        return contentPos;
    }
    
    
    internal static int LargestFactor(int value)
    {
        
        for (var i = 2; i < value; i++)
        {
            if (value % i == 0)
                return value / i;
        }

        return 1;

    }
}
