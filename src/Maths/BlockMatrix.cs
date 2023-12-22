#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElectroSim.Maths;

internal class BlockMatrixBlock<T> where T : EqualityComparer<T>
{
    /// <summary>
    /// Absolute position of the BlockMatrixBlock
    /// </summary>
    internal readonly Vec2Long AbsolutePos;
    /// <summary>
    /// Size of the BlockMatrixBlock
    /// </summary>
    internal readonly Vec2Long BlockSize;

    public bool IsEmpty { get; protected set; }
    
    protected readonly T DefaultValue;

    protected BlockMatrixBlock(T defaultValue, Vec2Long? absolutePos, Vec2Long blockSize)
    {
        DefaultValue = defaultValue;
        BlockSize = blockSize;

        AbsolutePos = absolutePos ?? -blockSize/2;
    }

    internal virtual bool Add(Vec2Long targetPos, T value)
    {
        return false;
    }
    
    // public virtual bool Remove(Vec2Long targetPos)
    // {
    //     return false;
    // }
    
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

    public virtual StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        return new StringBuilder();
    }
}


internal class BlockMatrix<T> : BlockMatrixBlock<T> where T : EqualityComparer<T>
{
    /// <summary>
    /// 2D Array containing all of the sub-blocks
    /// </summary>
    private readonly BlockMatrixBlock<T>[,] _subBlocks;

    /// <summary>
    /// Size of one of the contained blocks
    /// </summary>
    private readonly Vec2Long _subBlockSize;

    /// <summary>
    /// Amount of contained blocks
    /// </summary>
    private readonly Vec2Long _subBlockCount;

    
    public BlockMatrix(T defaultValue, Vec2Long blockSize, Vec2Long? blockAbsolutePos = null, T? populateValue = null) : base(defaultValue, blockAbsolutePos, blockSize)
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
        
        var absolutePos = blockAbsolutePos ?? -blockSize/2;

        var value = populateValue ?? defaultValue;

        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                var nextBlockAbsolutePos = AbsolutePos + (new Vec2Long(x, y) * _subBlockSize);
                
                _subBlocks[x, y] = new BlockMatrixValue<T>(defaultValue, nextBlockAbsolutePos, _subBlockSize, value);
            }
        }
    }

    /// <summary>
    /// Gets a value from the matrix
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vec2Long targetPos = default)
    {
        var nextBlockPos = GetNextBlockPos(targetPos, out var newTargetPos);
        var nextBlock = _subBlocks[nextBlockPos.X, nextBlockPos.Y];
        
        return nextBlock.Get(newTargetPos);
    }

    public T? this[long x, long y]
    {
        get => Get(new Vec2Long(x, y));
        set => Add(new Vec2Long(x, y), value ?? DefaultValue);
    }
    
    public T? this[Vec2Long pos]
    {
        get => Get(pos);
        set => Add(pos, value ?? DefaultValue);
    }

    /// <summary>
    /// Adds/sets a value in the matrix
    /// </summary>
    /// <param name="targetPos">the position of the element to set, relative to this matrix</param>
    /// <param name="value">the value to set</param>
    /// <returns>a bool describing if the matrix was changed</returns>
    internal override bool Add(Vec2Long targetPos, T value)
    {
        var nextBlockPos = GetNextBlockPos(targetPos, out var newTargetPos);
        var nextBlock = _subBlocks[nextBlockPos.X, nextBlockPos.Y] ?? throw new Exception("Block was null");
        
        var nextBlockAbsolutePos = AbsolutePos + (nextBlockPos * _subBlockSize);
        
        // if part is a BlockMatrixValue of size 2+, change it to a BlockMatrix
        if (nextBlock is BlockMatrixValue<T> nextBlock2 && _subBlockSize >= new Vec2Long(2))
        {
            // Console.WriteLine(BlockSize);
            
            var nextBlockValue = nextBlock2.GetValue();
            
            nextBlock = new BlockMatrix<T>(DefaultValue, _subBlockSize, nextBlockAbsolutePos, nextBlockValue);
            
            _subBlocks[nextBlockPos.X, nextBlockPos.Y] = nextBlock;
        }
        
        // recursively add the value
        var success = nextBlock.Add(newTargetPos, value);
        
        // compression
         
        // for each subBlock,
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                // if it is a BlockMatrix,
                if (_subBlocks[x, y] is BlockMatrix<T> subBlockMatrix)
                {
                    // and all of its subBlocks are BlockMatrixValues,
                    if (!subBlockMatrix._subBlocks.Cast<BlockMatrixBlock<T>?>()
                            .Aggregate(true, (current, subSubBlock) => current & subSubBlock is BlockMatrixValue<T>))
                        continue;
                     
                    // check if all of these subBlocks are equal
                    if (subBlockMatrix._subBlocks.Cast<BlockMatrixValue<T>>().Distinct().Count() == 1)
                    {
                        // if so, replace the entire subBlock with a BlockMatrixValue of the correct size
                        _subBlocks[x, y] = new BlockMatrixValue<T>(DefaultValue, nextBlockAbsolutePos, _subBlockSize, value);
                    }
                }
            }
        }
        
        // if we added something, the matrix will no longer be empty
        if (success)
            IsEmpty = false;
        
        return success;
    }
    
    /*/// <summary>
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
    }*/

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

    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        const double scale = GameConstants.BlockMatrixSvgScale;
        
        var svgString = nullableSvgString ?? new StringBuilder(
            $"<svg " +
                $"viewBox=\"" +
                    $"{-BlockSize.X/2f * scale} " +
                    $"{-BlockSize.Y/2f * scale} " +
                    $"{BlockSize.X/2f * scale} " +
                    $"{BlockSize.Y/2f * scale}" +
                $"\">" +
            $"<svg/>"
            );
        
        var rect = $"<rect style=\"fill:#ffffff;fill-opacity:0;stroke:#000000;stroke-width:{Math.Min(BlockSize.X / 64d, 1)}\" " +
                   $"width=\"{BlockSize.X * scale}\" height=\"{BlockSize.Y * scale}\" " +
                   $"x=\"{AbsolutePos.X * scale}\" y=\"{AbsolutePos.Y * scale}\"/>";
        
        // insert the rectangle into the svg string, 6 characters before the end (taking into account the closing `</svg>` tag)
        svgString.Insert(svgString.Length-6, rect);
        
        // pass the call downwards
        foreach (var subBlock in _subBlocks)
        {
            subBlock.GetSvgMap(svgString);
        }
        
        return svgString;

    }
    
    
}

internal class BlockMatrixValue<T> : BlockMatrixBlock<T> where T : EqualityComparer<T>
{
    private T _value;
    
    public BlockMatrixValue(T defaultValue, Vec2Long absolutePos, Vec2Long blockSize, T value) : base(defaultValue, absolutePos, blockSize)
    {
        _value = value;
    }

    internal override bool Add(Vec2Long targetPos, T? value)
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
    
    public override StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null)
    {
        
        const double scale = GameConstants.BlockMatrixSvgScale;

        var svgString = nullableSvgString ?? new StringBuilder(
            $"<svg " +
            $"viewBox=\"" +
            $"{-BlockSize.X/2f * scale} " +
            $"{-BlockSize.Y/2f * scale} " +
            $"{BlockSize.X/2f * scale} " +
            $"{BlockSize.Y/2f * scale}" +
            $"\">" +
            $"<svg/>"
        );
        
        
        var fillColor = "#00ff00";

        if (_value == DefaultValue) fillColor = "#ff0000;fill-opacity:0.1";
        if (BlockSize == new Vec2Long(1)) fillColor = "#ffff00";
        
        var rect = $"<rect style=\"fill:{fillColor};stroke:#000000;stroke-width:{Math.Min(BlockSize.X / 64d, 1)}\" " +
                   $"width=\"{BlockSize.X * scale}\" height=\"{BlockSize.Y * scale}\" " +
                   $"x=\"{AbsolutePos.X * scale}\" y=\"{AbsolutePos.Y * scale}\"/>";
        
        // Console.WriteLine(AbsolutePos + " => " + fillColor);
        
        svgString.Insert(svgString.Length-6, rect);

        return svgString;
    }

    public Vec2Long GetPos()
    {
        return AbsolutePos;
    }
    
    public T GetValue()
    {
        return _value;
    }
    
    // overrides
    
    
    public static bool operator ==(BlockMatrixValue<T> a, BlockMatrixValue<T> b)
    {
        if (Equals(a, null) || Equals(b, null))
            return false;

        return a._value == b._value;
    }
    
    public static bool operator !=(BlockMatrixValue<T> a, BlockMatrixValue<T> b)
    {
        if (Equals(a, null) || Equals(b, null))
            return false;

        return a._value != b._value;
    }
    
    private bool Equals(BlockMatrixValue<T> other)
    {
        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((BlockMatrixValue<T>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(_value);
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

internal static class ResultComparison
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
