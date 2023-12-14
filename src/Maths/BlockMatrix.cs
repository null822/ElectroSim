#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths;

internal class BlockMatrixBlock<T>
{
    protected readonly Vector2 Pos;
    protected readonly Vector2 BlockSize;


    protected bool IsEmpty;
    
    protected readonly T? DefaultValue;


    protected BlockMatrixBlock(T? defaultValue, Vector2 pos, Vector2 blockSize)
    {
        DefaultValue = defaultValue;
        Pos = pos;
        BlockSize = blockSize;
    }

    public virtual bool Add(Vector2 targetPos, T? value, Vector2 absolutePos = default)
    {
        return false;
    }
    
    public virtual bool Remove(Vector2 targetPos)
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
    private readonly BlockMatrixBlock<T>?[,] _subBlocks;

    /// <summary>
    /// size of one of the contained blocks
    /// </summary>
    private readonly Vector2 _subBlockSize;

    private readonly Vector2 _subBlockCount;

    
    
    public BlockMatrix(T? defaultValue, Vector2 blockSize, Vector2 pos = new()) : base(defaultValue, pos, blockSize)
    {
        // calculate largest W/H factors
        var wLargestFactor = BlockMatrixUtil.LargestFactor((int)blockSize.X);
        var hLargestFactor = BlockMatrixUtil.LargestFactor((int)blockSize.Y);
        
        
        // instantiate _subBlockSize to be as large as possible, but smaller than the matrix, while keeping the matrix size divisible by it
        _subBlockSize = new Vector2(wLargestFactor, hLargestFactor);
        
        
        // instantiate _subBlocks to be of size: smallest non-1 (unless matrix size is prime) factor of passed size.
        // also store this value in a field for later use
        _subBlockCount = new Vector2((int)(blockSize.X / wLargestFactor), (int)(blockSize.X / hLargestFactor));
        _subBlocks = new BlockMatrixBlock<T>[(int)_subBlockCount.X, (int)_subBlockCount.X];
        
        Console.WriteLine(_subBlockCount);

    }


    /// <summary>
    /// Gets a value from the matrix.
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vector2 targetPos = default)
    {
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);

        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);
        
        // get the next block
        var nextBlock = _subBlocks[(int)blockPos.X, (int)blockPos.Y];
        
        // calculate the new targetPos
        var blockSign = new Vector2(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _subBlockSize / _subBlockCount;
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
        
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);
        
        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);

        // get the next block
        var nextBlock = _subBlocks[(int)blockPos.X, (int)blockPos.Y];
        
        // if part is null, create it
        if (nextBlock == null)
        {
            nextBlock = 
                _subBlockSize == Vector2.One ?
                    new BlockMatrixValue<T>(DefaultValue, absolutePos, targetPos, Vector2.One, value) :
                    new BlockMatrix<T>(DefaultValue, _subBlockSize, targetPos);

            _subBlocks[(int)blockPos.X, (int)blockPos.Y] = nextBlock;
        }

        // calculate the new targetPos
        var blockSign = new Vector2(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _subBlockSize / _subBlockCount;
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
    /// <param name="targetPos">the position to remove the value at</param>
    /// <returns>a bool describing if the matrix was changed</returns>
    public override bool Remove(Vector2 targetPos)
    {
        var success = false;
        
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);
        
        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);

        // get the next block
        var nextBlock = _subBlocks[(int)blockPos.X, (int)blockPos.Y];
        
        
        // if the block does not exist to begin with, nothing will change
        if (nextBlock == null) return false;
        
        // if part refers to a BlockMatrixContainer, pass the call downwards.
        if (nextBlock.GetType() == typeof(BlockMatrix<T>))
        {
            success = nextBlock.Remove(targetPos);
        }
        
        // otherwise, if targetPos refers to a BlockMatrixValue, remove it and flatten the matrix.
        else if (nextBlock.GetType() == typeof(BlockMatrixValue<T>))
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
        var array = new T?[(int)BlockSize.X, (int)BlockSize.Y];

        var half = BlockSize / 2f;
        
        for (var x = 0; x < (int)BlockSize.X; x++)
        {
            for (var y = 0; y < (int)BlockSize.Y; y++)
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

        foreach (var block in _subBlocks)
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

        foreach (var block in _subBlocks)
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
        
        foreach (var part in _subBlocks)
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
    
    public BlockMatrixValue(T? defaultValue, Vector2 absolutePos, Vector2 pos, Vector2 blockSize, T value) : base(defaultValue, pos, blockSize)
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
    internal static Vector2 GetBlockIndexFromPos(Vector2 localPos, Vector2 blockSize, Vector2 subBlockCount)
    {
        
        var e = new Vector2(
            (int)Math.Floor(localPos.X / blockSize.X) + subBlockCount.X/2f,
            (int)Math.Floor(localPos.Y / blockSize.Y) + subBlockCount.Y/2f
        );
        
        // Console.WriteLine(e);

        return e;

    }

    private static int SignedFloor(double d)
    {
        return d < 0 ? (int)Math.Ceiling(d) : (int)Math.Floor(d);
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
