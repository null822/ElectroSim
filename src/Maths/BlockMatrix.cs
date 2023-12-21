#nullable enable
using System;
using System.Linq;

namespace ElectroSim.Maths;

internal class BlockMatrixBlock<T>
{
    internal readonly Vec2Long AbsolutePos;
    internal readonly Vec2Long BlockSize;

    public bool IsEmpty { get; protected set; }
    
    protected readonly T? DefaultValue;


    protected BlockMatrixBlock(T? defaultValue, Vec2Long absolutePos, Vec2Long blockSize)
    {
        DefaultValue = defaultValue;
        AbsolutePos = absolutePos;
        BlockSize = blockSize;
    }

    internal virtual bool Add(Vec2Long targetPos, T? value, Vec2Long absolutePos = default)
    {
        return false;
    }
    
    public virtual bool Remove(Vec2Long targetPos)
    {
        return false;
    }
    
    internal virtual T? Get(Vec2Long targetPos = default)
    {
        return default;
    }

    /// <summary>
    /// Runs a lambda for each element. Optimised for the BlockMatrix structure.
    /// </summary>
    /// <param name="lambda">the lambda to run</param>
    /// <returns>the AND of all of the results of the lambdas</returns>
    public virtual bool All(Func<T, Vec2Long, bool> lambda)
    {
        return false;
    }
    
    /// <summary>
    /// Runs a lambda for each element. Optimised for the BlockMatrix structure.
    /// </summary>
    /// <param name="lambda">the lambda to run</param>
    /// <returns>the OR of all of the results of the lambdas</returns>
    public virtual bool Any(Func<T, Vec2Long, bool> lambda)
    {
        return false;
    }

    /// <summary>
    /// Runs the specified lambda for each element only if the pos lambda returns true when given the pos of the element.
    /// Only works if the pos lambda matches elements linearly ()
    /// </summary>
    /// <param name="range">range of elements to run the lambda at</param>
    /// <param name="run">the lambda to run at each valid element</param>
    /// <param name="resultComparison">the lambda to compare the results</param>
    /// <param name="resultStart">the starting value of the result</param>
    /// <returns>the result of comparing all of the results of the run lambda</returns>
    public virtual bool? InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run,
        Func<bool?, bool?, bool> resultComparison, bool resultStart)
    {
        return null;
    }

}


internal class BlockMatrix<T> : BlockMatrixBlock<T>
{
    /// <summary>
    /// 2D Array containing all of the sub-blocks
    /// </summary>
    private readonly BlockMatrixBlock<T>?[,] _subBlocks;

    /// <summary>
    /// Size of one of the contained blocks
    /// </summary>
    private readonly Vec2Long _subBlockSize;

    /// <summary>
    /// Amount of contained blocks
    /// </summary>
    private readonly Vec2Long _subBlockCount;

    
    public BlockMatrix(T? defaultValue, Vec2Long blockSize, Vec2Long absolutePos = new()) : base(defaultValue, absolutePos, blockSize)
    {
        // calculate largest W/H factors
        var wLargestFactor = BlockMatrixUtil.LargestFactor(blockSize.X);
        var hLargestFactor = BlockMatrixUtil.LargestFactor(blockSize.Y);
        
        
        // instantiate _subBlockSize to be as large as possible, but smaller than the matrix, while keeping the matrix size divisible by it
        _subBlockSize = new Vec2Long(wLargestFactor, hLargestFactor);
        
        
        // instantiate _subBlocks to be of size: smallest non-1 (unless matrix size is prime) factor of passed size.
        // also store this value in a field for later use
        _subBlockCount = new Vec2Long(blockSize.X / wLargestFactor, blockSize.X / hLargestFactor);
        _subBlocks = new BlockMatrixBlock<T>[_subBlockCount.X, _subBlockCount.X];
        
    }


    /// <summary>
    /// Gets a value from the matrix
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vec2Long targetPos = default)
    {
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);

        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);
        
        // get the next block
        var nextBlock = _subBlocks[blockPos.X, blockPos.Y];
        
        // calculate the new targetPos
        var blockSign = new Vec2Long(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _subBlockSize / _subBlockCount;
        var newTargetPos = targetPos - (nextBlockBlockSize * blockSign);

        return nextBlock == null ? DefaultValue : nextBlock.Get(newTargetPos);
    }

    public T? this[long x, long y]
    {
        get => Get(new Vec2Long(x, y));
        set => Add(new Vec2Long(x, y), value, new Vec2Long(x, y));
    }
    
    public T? this[Vec2Long pos]
    {
        get => Get(pos);
        set => Add(pos, value, pos);
    }
    
    /// <summary>
    /// Adds/sets a value in the matrix
    /// </summary>
    /// <param name="targetPos">the position of the element to set, relative to this matrix</param>
    /// <param name="value">the value to set</param>
    /// <param name="absolutePos">the absolute position of the element to set</param>
    /// <returns>a bool describing if the matrix was changed</returns>
    internal override bool Add(Vec2Long targetPos, T? value, Vec2Long absolutePos = default)
    {
        if (value == null)
            return Remove(targetPos);

        if (value.Equals(DefaultValue))
            return Remove(targetPos);
        
        var nextBlockPos = GetNextBlockPos(targetPos, out var newTargetPos);
        
        var nextBlock = _subBlocks[nextBlockPos.X, nextBlockPos.Y];
        
        // if part is null, create it
        if (nextBlock == null)
        {
            nextBlock = 
                _subBlockSize == new Vec2Long(1) ?
                    new BlockMatrixValue<T>(DefaultValue, absolutePos, new Vec2Long(1), value) :
                    new BlockMatrix<T>(DefaultValue, _subBlockSize, absolutePos);

            _subBlocks[nextBlockPos.X, nextBlockPos.Y] = nextBlock;
        }

        // // calculate the new targetPos
        // var blockSign = new Vec2Long(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        // var nextBlockBlockSize = _subBlockSize / _subBlockCount;
        // var newTargetPos = targetPos - (nextBlockBlockSize * blockSign);
        
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
    public override bool Remove(Vec2Long targetPos)
    {
        var success = false;
        
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);
        
        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);

        GetNextBlockPos(targetPos, out var newTargetPos);
        
        // get the next block
        var nextBlock = _subBlocks[blockPos.X, blockPos.Y];
        
        // if the block does not exist to begin with, nothing will change
        if (nextBlock == null) return false;
        
        // if block is a BlockMatrix, pass the call downwards.
        if (nextBlock.GetType() == typeof(BlockMatrix<T>))
        {
            success = nextBlock.Remove(newTargetPos);
        }
        
        // otherwise, if the block is a BlockMatrixValue, remove it and flatten the matrix.
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

    public override bool All(Func<T, Vec2Long, bool> lambda)
    {
        return _subBlocks.OfType<BlockMatrixBlock<T>>().Aggregate(
            true,
            (current, subBlock) => current & subBlock.All(lambda));
    }
    
    public override bool Any(Func<T, Vec2Long, bool> lambda)
    {
        return _subBlocks.OfType<BlockMatrixBlock<T>>().Aggregate(
            false,
            (current, subBlock) => current | subBlock.All(lambda));
    }

    public override bool? InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run, Func<bool?, bool?, bool> resultComparison, bool resultStart)
    {
        var retVal = resultStart;
        
        foreach (var subBlock in _subBlocks)
        {
            if (subBlock == null) continue; // if the subBlock is null, continue
            if (subBlock.IsEmpty) continue; // if the subBlock is empty, continue
            
            var subBlockRect = new Range2D(
                subBlock.AbsolutePos.X, 
                subBlock.AbsolutePos.Y, 
                subBlock.AbsolutePos.X + subBlock.BlockSize.X, 
                subBlock.AbsolutePos.Y + subBlock.BlockSize.Y);
            
            if (range.Overlaps(subBlockRect))
            {
                retVal = resultComparison.Invoke(retVal, subBlock.InvokeRanged(range, run, resultComparison, resultStart));
            }
            
        }

        return retVal;
    }

    private bool CheckEmpty()
    {
        var isEmpty = true;
        
        foreach (var part in _subBlocks)
        {
            // ignore null or empty parts
            if (part == null) continue;
            
            // if it is a BlockMatrix pass the call downwards
            if (part.GetType() == typeof(BlockMatrix<T>))
            {
                isEmpty &= ((BlockMatrix<T>)part).CheckEmpty();
            }
            
            // otherwise, if it is a BlockMatrixValue, return false as the matrix is not empty
            else if (part.GetType() == typeof(BlockMatrixValue<T>))
            {
                return !((BlockMatrixValue<T>)part).Get()!.Equals(DefaultValue);
            }
        }

        return isEmpty;
    }

    private Vec2Long GetNextBlockPos(Vec2Long targetPos, out Vec2Long newTargetPos)
    {
        // get the targetPos of the block the target is in
        var blockPos = BlockMatrixUtil.GetBlockIndexFromPos(targetPos, BlockSize, _subBlockCount);

        // throw exception if the position is out of bounds
        if (blockPos.X < 0 || blockPos.X > _subBlockCount.X || blockPos.Y < 0 || blockPos.Y > _subBlockCount.Y)
            throw new Exception("Position " + targetPos + " is outside BlockMatrix bounds of +/- " + BlockSize.X + "x" + BlockSize.Y);
        
        // calculate the new targetPos
        var blockSign = new Vec2Long(blockPos.X <= 0 ? -1 : 1, blockPos.Y <= 0 ? -1 : 1);
        var nextBlockBlockSize = _subBlockSize / _subBlockCount;
        newTargetPos = targetPos - (nextBlockBlockSize * blockSign);

        return blockPos;
    }
    
    
}

internal class BlockMatrixValue<T> : BlockMatrixBlock<T>
{
    private T _value;
    // private readonly Vec2Long _absolutePos;
    
    public BlockMatrixValue(T? defaultValue, Vec2Long absolutePos, Vec2Long blockSize, T value) : base(defaultValue, absolutePos, blockSize)
    {
        // _absolutePos = absolutePos;
        _value = value;
    }

    internal override bool Add(Vec2Long targetPos, T? value, Vec2Long absolutePos = default)
    {
        if (value == null)
            return false;
        
        _value = value;
        return true;
    }

    internal override T Get(Vec2Long targetPos = default)
    {
        return _value;
    }

    public override bool All(Func<T, Vec2Long, bool> lambda)
    {
        return Invoke(lambda);
    }
    
    public override bool Any(Func<T, Vec2Long, bool> lambda)
    {
        return Invoke(lambda);
    }

    private bool Invoke(Func<T, Vec2Long, bool> lambda)
    {
        return lambda.Invoke(_value, AbsolutePos);
    }
    
    public override bool? InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run, Func<bool?, bool?, bool> resultComparison, bool resultStart)
    {
        
        var subBlockRect = new Range2D(
            AbsolutePos.X,
            AbsolutePos.Y, 
            AbsolutePos.X + BlockSize.X, 
            AbsolutePos.Y + BlockSize.Y);
        
        if (range.Overlaps(subBlockRect))
        {
            return run.Invoke(_value, AbsolutePos);
        }
        
        return null;
    }

    public Vec2Long GetPos()
    {
        return AbsolutePos;
    }
}

internal static class BlockMatrixUtil
{
    internal static Vec2Long GetBlockIndexFromPos(Vec2Long localPos, Vec2Long blockSize, Vec2Long subBlockCount)
    {
        return new Vec2Long(
            (long)Math.Floor((double)localPos.X / blockSize.X) + subBlockCount.X/2,
            (long)Math.Floor((double)localPos.Y / blockSize.Y) + subBlockCount.Y/2
        );
    }
    
    internal static long LargestFactor(long value)
    {
        for (long i = 2; i < value; i++)
        {
            if (value % i == 0)
                return value / i;
        }

        return 1;

    }
    
}

static class ResultComparison
{
    public static readonly Func<bool?, bool?, bool> Or = (a, b) =>
    {
        if (a == null && b == null) return false;
        if (a == null && b != null) return (bool)b;
        if (a != null && b == null) return (bool)a;
        if (a != null && b != null) return (bool)a | (bool)b;
        
        return false;
    };
    
    public static readonly  Func<bool?, bool?, bool> And = (a, b) =>
    {
        if (a == null && b == null) return false;
        if (a == null && b != null) return (bool)b;
        if (a != null && b == null) return (bool)a;
        if (a != null && b != null) return (bool)a & (bool)b;
        
        return false;
    };
    
    
}
