#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ElectroSim.Maths.BlockMatrix;

internal abstract class BlockMatrixBlock<T> where T : class, IBlockMatrixElement<T>
{
    /// <summary>
    /// Absolute position of the BlockMatrixBlock
    /// </summary>
    internal readonly Vec2Long AbsolutePos;
    /// <summary>
    /// Size of the BlockMatrixBlock
    /// </summary>
    internal readonly Vec2Long BlockSize;
    
    /// <summary>
    /// The default value of the BlockMatrix (everything is default by default)
    /// </summary>
    protected readonly T DefaultValue;

    protected BlockMatrixBlock(T defaultValue, Vec2Long? absolutePos, Vec2Long blockSize)
    {
        DefaultValue = defaultValue;
        BlockSize = blockSize;

        AbsolutePos = absolutePos ?? -blockSize/2;
    }

    /// <summary>
    /// Sets a single value in the BlockMatrix
    /// </summary>
    /// <param name="targetPos">the position of the value to set, relative to the BlockMatrix called in</param>
    /// <param name="value">the value to set</param>
    /// <returns>a bool describing if the BlockMatrix was changed (ignoring compression)</returns>
    internal abstract bool Set(Vec2Long targetPos, T value);

    /// <summary>
    /// Sets an area of values to one single value in the BlockMatrix
    /// </summary>
    /// <param name="targetRange">the area of values to set, relative to the BlockMatrix called in</param>
    /// <param name="value">the value to set</param>
    /// <returns>a bool describing if the BlockMatrix was changed (ignoring compression)</returns>
    internal abstract bool Set(Range2D targetRange, T value);

    /// <summary>
    /// Gets a value from the BlockMatrix
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    internal abstract T? Get(Vec2Long targetPos);

    /// <summary>
    /// Runs the specified lambda for each element residing in the supplied range
    /// </summary>
    /// <param name="range">range of positions of elements to run the lambda for</param>
    /// <param name="run">the lambda to run at each element</param>
    /// <param name="rc">a ResultComparison to compare the results</param>
    /// <param name="excludeDefault">whether to exclude all elements with the default value</param>
    /// <returns>the result of comparing all of the results of the run lambdas</returns>
    public abstract bool InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run,
        ResultComparison rc, bool excludeDefault = false);

    /// <summary>
    /// Creates an SVG representing the current structure of the entire BlockMatrix
    /// </summary>
    /// <param name="nullableSvgString">optional parameter, used internally to pass the SVG around. Will likely break if changed.</param>
    /// <returns>the contents of an SVG file, ready to be saved</returns>
    public abstract StringBuilder GetSvgMap(StringBuilder? nullableSvgString = null);

    /// <summary>
    /// Serializes the BlockMatrixBlock. See:
    /// <code>src/BlockMatrix-Format.md</code>
    /// </summary>
    /// <param name="tree">an empty stream to which the tree section will be written to</param>
    /// <param name="data">an empty stream to which the data section will be written to</param>
    internal virtual void SerializeBlockMatrix(Stream tree, Stream data)
    {
        // write the AbsolutePos,
        tree.Write(BitConverter.GetBytes(AbsolutePos.X));
        tree.Write(BitConverter.GetBytes(AbsolutePos.Y));
        
        // and BlockSize to the tree
        tree.Write(BitConverter.GetBytes(BlockSize.X));
        tree.Write(BitConverter.GetBytes(BlockSize.Y));
    }

    /// <summary>
    /// Returns the BlockMatrixBlock represented as a Range2D
    /// </summary>
    /// <returns></returns>
    internal Range2D GetRange()
    {
        return new Range2D(
            AbsolutePos.X, 
            AbsolutePos.Y,
            AbsolutePos.X + BlockSize.X,
            AbsolutePos.Y + BlockSize.Y);
    }
    
}

internal class BlockMatrix<T> : BlockMatrixBlock<T> where T : class, IBlockMatrixElement<T>
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
        
        // populate the _subBlocks array with either the defaultValue or a supplied populateValue if present
        var value = populateValue ?? defaultValue;

        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                var nextBlockAbsolutePos = AbsolutePos + new Vec2Long(x, y) * _subBlockSize;
                
                _subBlocks[x, y] = new BlockMatrixValue<T>(defaultValue, nextBlockAbsolutePos, _subBlockSize, value);
            }
        }
    }
    
    /// <summary>
    /// Gets a value from the matrix
    /// </summary>
    /// <param name="targetPos">the position to get the value from</param>
    /// <returns>the value</returns>
    internal override T? Get(Vec2Long targetPos)
    {
        var nextBlockPos = GetNextBlockPos(targetPos, out var newTargetPos);
        var nextBlock = _subBlocks[nextBlockPos.X, nextBlockPos.Y];
        
        return nextBlock.Get(newTargetPos);
    }
    
    /// <summary>
    /// What do you think this does?
    /// </summary>
    public T? this[long x, long y]
    {
        get => Get(new Vec2Long(x, y));
        set => Set(new Vec2Long(x, y), value ?? DefaultValue);
    }
    
    /// <summary>
    /// What do you think this does?
    /// </summary>
    public T? this[Vec2Long pos]
    {
        get => Get(pos);
        set => Set(pos, value ?? DefaultValue);
    }
    
    /// <summary>
    /// What do you think this does?
    /// </summary>
    public T? this[Range2D range]
    {
        // get => throw new NotImplementedException();
        set => Set(range, value ?? DefaultValue);
    }
    
    internal override bool Set(Range2D targetRange, T value)
    {
        // if the range refers to a single point, use the Set(Vec2Long, T) method instead since it is more efficient for single positions
        if (targetRange.GetArea() == 1)
        {
            return Set(new Vec2Long(targetRange.MinX, targetRange.MinY), value);
        }
        
        var success = false;
        
        // for every subBlock,
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                var subBlock = _subBlocks[x, y];
                
                var subBlockRange = subBlock.GetRange();

                // check if the supplied targetRange overlaps with the subBlock.
                if (targetRange.Overlaps(subBlockRange))
                {
                    var nextBlockAbsolutePos = AbsolutePos + (new Vec2Long(x, y) * _subBlockSize);

                    // if the supplied targetRange completely contains the subBlock,
                    if (targetRange.Contains(subBlockRange))
                    {

                        _subBlocks[x, y] =
                            new BlockMatrixValue<T>(DefaultValue, nextBlockAbsolutePos, _subBlockSize, value);

                        success = true;
                        continue;
                    }
                    
                    // otherwise, pass the call downwards:
                    // if the subBlock is a BlockMatrixValue that is not fully contained within the targetRange,
                    if (subBlock is BlockMatrixValue<T> subBlockValue && !targetRange.Contains(subBlock.GetRange()))
                    {
                        // split the BlockMatrixValue into a BlockMatrix,
                        _subBlocks[x, y] = new BlockMatrix<T>(DefaultValue, _subBlockSize, nextBlockAbsolutePos, subBlockValue.GetValue());
                        
                        // and then pass the call downwards,
                        success |= _subBlocks[x, y].Set(targetRange, value);
                    }
                    // otherwise, just pass the call downwards
                    else
                    {
                        success |= _subBlocks[x, y].Set(targetRange, value);
                    }
                }
            }
        }
        
        // run a compression pass over everything that was/could have been changed
        Compress();
        
        return success;
    }
    
    internal override bool Set(Vec2Long targetPos, T value)
    {
        var nextBlockPos = GetNextBlockPos(targetPos, out var newTargetPos);
        var nextBlock = _subBlocks[nextBlockPos.X, nextBlockPos.Y] ?? throw new Exception("Block was null");
        
        var nextBlockAbsolutePos = AbsolutePos + (nextBlockPos * _subBlockSize);
        
        // if part is a BlockMatrixValue of size > 1, change it to a BlockMatrix
        if (nextBlock is BlockMatrixValue<T> nextBlockValue && _subBlockSize > new Vec2Long(1))
        {
            nextBlock = new BlockMatrix<T>(DefaultValue, _subBlockSize, nextBlockAbsolutePos, nextBlockValue.GetValue());
            
            _subBlocks[nextBlockPos.X, nextBlockPos.Y] = nextBlock;
        }
        
        // recursively add the value
        var success = nextBlock.Set(newTargetPos, value);
        
        // compression
        Compress();
        
        return success;
    }
    
    public override bool InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        var retVal = rc.StartingValue;
        
        foreach (var subBlock in _subBlocks)
        {
            var subBlockRect = new Range2D(
                subBlock.AbsolutePos.X, 
                subBlock.AbsolutePos.Y, 
                subBlock.AbsolutePos.X + subBlock.BlockSize.X, 
                subBlock.AbsolutePos.Y + subBlock.BlockSize.Y);
            
            if (range.Overlaps(subBlockRect))
            {
                retVal = rc.Comparator
                    .Invoke(retVal, subBlock.InvokeRanged(range, run, rc, excludeDefault));
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
            svgString = subBlock.GetSvgMap(svgString);
        }
        
        return svgString;

    }

    /// <summary>
    /// Serializes the BlockMatrix into a format described in:
    /// <code>src/BlockMatrix-Format.md</code>
    /// <param name="stream">the stream to write to</param>
    /// </summary>
    public void Serialize(Stream stream)
    {
        // initialize the tree and data streams as MemoryStreams
        var treeStream = new MemoryStream();
        var dataStream = new MemoryStream();

        // serialize the current BlockMatrix
        SerializeBlockMatrix(treeStream, dataStream);

        // reset stream positions
        treeStream.Position = 0;
        dataStream.Position = 0;
        
        // get length of the tree/data streams
        var treeLen = treeStream.Length;
        var dataLen = dataStream.Length;

        
        // write the header to the stream

        // add contents to the header
        stream.Write(BitConverter.GetBytes(BlockSize.X)); // width
        stream.Write(BitConverter.GetBytes(BlockSize.Y)); // width
        stream.Write(BitConverter.GetBytes(T.SerializeLength)); // element size (bytes)
        stream.Write(BitConverter.GetBytes(treeStream.Length + 32)); // pointer to the start of the data section
        
        
        // write the tree/data sections to the stream and dispose all the variables that we can

        var tree = new Span<byte>(new byte[treeStream.Length]);
        var treeRead = treeStream.Read(tree);
        treeStream.Dispose();
        stream.Write(tree);
        tree.Clear();

        var data = new Span<byte>(new byte[dataStream.Length]);
        var dataRead = dataStream.Read(data);
        dataStream.Dispose();
        stream.Write(data);
        data.Clear();

        if (treeRead != treeLen)
            Util.Error($"Tree Section was not fully saved ({treeRead}/{treeStream.Length} bytes written)");

        if (dataRead != dataLen)
            Util.Error($"Data Section was not fully saved ({dataRead}/{dataStream.Length} bytes written)");
        
    }


    internal override void SerializeBlockMatrix(Stream tree, Stream data)
    {
        // write the identifier for a BlockMatrix to the tree stream
        tree.Write(new byte[]{ 1 });
        
        // write the default data for a BlockMatrixBlock to the tree stream
        base.SerializeBlockMatrix(tree, data);
        
        // pass the call downwards, causing everything to be serialized in the correct order
        foreach (var subBlock in _subBlocks)
        {
            subBlock.SerializeBlockMatrix(tree, data);
        }
        
        // write the identifier for the end of the BlockMatrix to the tree stream
        tree.Write(new byte[]{ 1 });
    }

    private void Compress()
    {
        // for each subBlock,
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                // get absolute pos of this block
                var currentBlockAbsolutePos = AbsolutePos + new Vec2Long(x, y) * _subBlockSize;
                
                // if it is a BlockMatrix,
                if (_subBlocks[x, y] is BlockMatrix<T> subBlockMatrix)
                {
                    var subSubBlocks = subBlockMatrix._subBlocks;

                    var firstValue = subSubBlocks[0, 0] is BlockMatrixValue<T> e ? e.GetValue() : null;
                    
                    if (firstValue == null)
                        continue;
                    
                    // check if all of these subBlocks are equal
                    var allEqual = true;
                    foreach (var subSubBlock in subSubBlocks)
                    {
                        if (subSubBlock is not BlockMatrixValue<T> subSubBlockValue)
                        {
                            allEqual = false;
                            break;
                        }
                            
                        allEqual &= subSubBlockValue.GetValue().Equals(firstValue);

                        if (!allEqual)
                        {
                            break;
                        }
                    }
                    
                    // if so,
                    if (allEqual)
                    {
                        //replace the entire subBlock with a BlockMatrixValue of the correct size
                        _subBlocks[x, y] = new BlockMatrixValue<T>(DefaultValue, currentBlockAbsolutePos, _subBlockSize, firstValue);
                    }
                }
            }
        }
    }
    
    
}

internal class BlockMatrixValue<T> : BlockMatrixBlock<T> where T : class, IBlockMatrixElement<T>
{
    private T _value;
    
    public BlockMatrixValue(T defaultValue, Vec2Long absolutePos, Vec2Long blockSize, T value) : base(defaultValue, absolutePos, blockSize)
    {
        _value = value;
    }

    internal override bool Set(Vec2Long targetPos, T? value)
    {
        if (value == null)
            return false;
        
        _value = value;
        return true;
    }
    
    internal override bool Set(Range2D targetRange, T? value)
    {
        if (value == null)
            return false;

        if (targetRange.Contains(GetRange()))
        {
            _value = value;
            return true;
        }
        
        return false;
    }

    internal override T Get(Vec2Long targetPos)
    {
        return _value;
    }
    
    public override bool InvokeRanged(Range2D range, Func<T, Vec2Long, bool> run, ResultComparison rc, bool excludeDefault = false)
    {
        if (excludeDefault && _value == DefaultValue) return false;
        
        // get the Range2D of this BlockMatrixValue
        var blockRange = new Range2D(
            AbsolutePos.X,
            AbsolutePos.Y, 
            AbsolutePos.X + BlockSize.X, 
            AbsolutePos.Y + BlockSize.Y);
        
        // if the supplied range overlaps with subBlockRange,
        if (range.Overlaps(blockRange))
        {
            // get the overlap rectangle of this BlockMatrixValue, and the supplied range
            var overlap = range.Overlap(blockRange);
            
            // invoke the run Func for every discrete position in the overlap

            var result = rc.StartingValue;
            
            for (var x = overlap.MinX; x < overlap.MaxX; x++)
            {
                for (var y = overlap.MinY; y < overlap.MaxY; y++)
                {
                    result = rc.Comparator.Invoke(
                        result,
                        run.Invoke(_value, new Vec2Long(x, y))
                        );
                }
            }
            
            return result;
        }
        
        return false;
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

        if (BlockSize == new Vec2Long(1)) fillColor = "#ffff00";
        if (_value == DefaultValue) fillColor = "#ff0000;fill-opacity:0.1";

        var rect = $"<rect style=\"fill:{fillColor};stroke:#000000;stroke-width:{Math.Min(BlockSize.X / 64d, 1)}\" " +
                   $"width=\"{BlockSize.X * scale}\" height=\"{BlockSize.Y * scale}\" " +
                   $"x=\"{AbsolutePos.X * scale}\" y=\"{AbsolutePos.Y * scale}\"/>";
        
        svgString.Insert(svgString.Length-6, rect);

        return svgString;
    }
    
    internal override void SerializeBlockMatrix(Stream tree, Stream data)
    {
        // write the identifier for a BlockMatrixValue to the tree stream
        tree.Write(new byte[] { 2 });
        
        // write the default data for a BlockMatrixBlock to the tree stream
        base.SerializeBlockMatrix(tree, data);

        // write, and get the pointer of, the value in the data stream
        var pointer = (int)data.Position / T.SerializeLength;
        data.Write(_value.Serialize());
        
        // write the pointer of the value to the tree stream
        tree.Write(BitConverter.GetBytes(pointer));
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

public static class ResultComparisons
{
    public static readonly ResultComparison Or = new ResultComparisonOr();
    public static readonly ResultComparison And = new ResultComparisonAnd();
}

public abstract class ResultComparison
{
    public abstract Func<bool, bool, bool> Comparator { get; }
    public abstract bool StartingValue { get; }

}

public class ResultComparisonOr : ResultComparison
{
    public override Func<bool, bool, bool> Comparator => (a, b) => a || b;
    public override bool StartingValue => false;
}

public class ResultComparisonAnd : ResultComparison
{
    public override Func<bool, bool, bool> Comparator => (a, b) => a && b;
    public override bool StartingValue => true;
}
