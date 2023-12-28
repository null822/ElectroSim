﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static ElectroSim.Maths.BlockMatrix.BlockMatrixUtil;

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
    /// Deserializes a serialized BlockMatrixBlock. See:
    /// <code>src/BlockMatrix-Format.md</code>
    /// </summary>
    /// <param name="tree">a stream containing the tree section</param>
    /// <param name="data">a stream containing the data section</param>
    internal virtual void DeserializeBlockMatrix(Stream tree, Stream data)
    {
        // // write the AbsolutePos,
        // tree.Write(BitConverter.GetBytes(AbsolutePos.X));
        // tree.Write(BitConverter.GetBytes(AbsolutePos.Y));
        //
        // // and BlockSize to the tree
        // tree.Write(BitConverter.GetBytes(BlockSize.X));
        // tree.Write(BitConverter.GetBytes(BlockSize.Y));
    }

    /// <summary>
    /// Deserializes a serialized BlockMatrixBlock. See:
    /// <code>src/BlockMatrix-Format.md</code>
    /// </summary>
    /// <param name="tree">a stream containing the tree section</param>
    /// <param name="data">a stream containing the data section</param>
    /// <param name="index">the index within _subBlocks of the element to serialize</param>
    internal virtual void SerializeBlockMatrix(Stream tree, Stream data, Vec2Long index)
    {
        // write the index,
        tree.Write(BitConverter.GetBytes((uint)index.X));
        tree.Write(BitConverter.GetBytes((uint)index.Y));
        
        /*// and BlockSize, to the tree
        tree.Write(BitConverter.GetBytes(BlockSize.X));
        tree.Write(BitConverter.GetBytes(BlockSize.Y));*/
        
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
        var wLargestFactor = LargestFactor(blockSize.X);
        var hLargestFactor = LargestFactor(blockSize.Y);
        
        // instantiate _subBlockSize to be as large as possible, but smaller than the matrix, while keeping the matrix size divisible by it
        _subBlockSize = new Vec2Long(wLargestFactor, hLargestFactor);
        
        // instantiate _subBlocks to be of size: smallest non-1 (unless matrix size is prime) factor of passed size.
        // also store this value in a field for later use
        _subBlockCount = new Vec2Long(blockSize.X / wLargestFactor, blockSize.Y / hLargestFactor);
        _subBlocks = new BlockMatrixBlock<T>[_subBlockCount.X, _subBlockCount.Y];
        
        // populate the _subBlocks array with either the defaultValue or the supplied populateValue if it is not null
        var value = populateValue ?? defaultValue;
        
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                var subBlockAbsolutePos = AbsolutePos + new Vec2Long(x, y) * _subBlockSize;
                
                _subBlocks[x, y] = new BlockMatrixValue<T>(defaultValue, subBlockAbsolutePos, _subBlockSize, value);
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
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        // write the DefaultValue into the first position of the data stream
        data.Write(DefaultValue.Serialize());

        // serialize the entire BlockMatrix, skipping the root block (serialize the subBlocks of the root block)
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                _subBlocks[x, y].SerializeBlockMatrix(tree, data, new Vec2Long(x, y));
            }
        }
        
        // write the header values to the output stream
        stream.Write(BitConverter.GetBytes(BlockSize.X)); // width
        stream.Write(BitConverter.GetBytes(BlockSize.Y)); // width
        stream.Write(BitConverter.GetBytes(T.SerializeLength)); // element size (bytes)
        stream.Write(BitConverter.GetBytes((uint)(tree.Length + 24))); // pointer to the start of the data section
        
        // reset tree/data stream positions
        tree.Position = 0;
        data.Position = 0;
        
        // write the tree/data sections to the output stream
        tree.WriteTo(stream);
        data.WriteTo(stream);
        
        // dispose the original streams
        tree.Dispose();
        data.Dispose();
    }
    
    internal override void SerializeBlockMatrix(Stream tree, Stream data, Vec2Long index)
    {
        // write the identifier for a BlockMatrix to the tree stream
        tree.Write(new byte[]{ 1 });
        
        // write the default data for a BlockMatrixBlock to the tree stream
        base.SerializeBlockMatrix(tree, data, index);
        
        // pass the call downwards, causing everything to be serialized in order
        for (var x = 0; x < _subBlockCount.X; x++)
        {
            for (var y = 0; y < _subBlockCount.Y; y++)
            {
                _subBlocks[x, y].SerializeBlockMatrix(tree, data, new Vec2Long(x, y));
            }
        }
        
        // write the identifier for the end of the BlockMatrix to the tree stream
        tree.Write(new byte[]{ 0 });
    }
    
    /// <summary>
    /// Deserializes the supplied stream into a BlockMatrix and returns it
    /// </summary>
    /// <param name="stream">the stream containing the serialized BlockMatrix</param>
    /// <returns>the deserialized BlockMatrix</returns>
    public static BlockMatrix<T> Deserialize(Stream stream)
    {
        // get the header
        var header = ReadStream(stream, 24, "header");
        
        // get values from the header
        var blockSize = new Vec2Long(BitConverter.ToInt64(header[..8]), BitConverter.ToInt64(header[8..16]));
        var elementSize = BitConverter.ToUInt32(header[16..20]);
        var dataPointer = BitConverter.ToUInt32(header[20..24]);
        
        // warn if the element sizes do not match
        if (elementSize != T.SerializeLength)
            Util.Warn($"Element Sizes do not match! File contains element size of {elementSize} " +
                      $"but is being deserialized with element size of {T.SerializeLength}.");
        
        // read the tree and data sections from the stream
        var treeSpan = ReadStream(stream, dataPointer - 24, "tree section");
        var dataSpan = ReadStream(stream, stream.Length - dataPointer, "data section");
        
        // create streams for the sections
        var tree = new MemoryStream();
        var data = new MemoryStream();
        
        // write the sections to their streams
        tree.Write(treeSpan);
        data.Write(dataSpan);

        // reset positions back to 0 within the streams
        tree.Position = 0;
        data.Position = 0;
        
        // get default value from the data stream
        var defaultValueBytes = ReadStream(data, elementSize, "default value");
        var defaultValue = T.Deserialize(defaultValueBytes);
        
        // create the BlockMatrix
        var blockMatrix = new BlockMatrix<T>(defaultValue, blockSize);
        
        // deserialize the blockMatrix
        blockMatrix.DeserializeBlockMatrix(tree, data);
        
        // dispose the tree and data streams
        tree.Dispose();
        data.Dispose();
    
        // return the BlockMatrix
        return blockMatrix;
    }
    
    internal override void DeserializeBlockMatrix(Stream tree, Stream data)
    {
        while (true)
        {
            // if we have reached the end of the tree stream, stop deserializing
            if (tree.Position == tree.Length)
                return;
            
            // read the id
            var id = ReadStream(tree, 1, "id");
            
            // if the id is 0, stop deserializing into this BlockMatrix
            if (id[0] == 0) return;

            // read the index the blockMatrixBlock within the parent _subBlocks array (4x2 (8) bytes)
            var indexBytes = ReadStream(tree, 8, "BlockMatrix structure");
            
            // get the index
            var index = new Vec2Long(
                BitConverter.ToUInt32(indexBytes[..4]),
                BitConverter.ToUInt32(indexBytes[4..8]));
            
            // calculate the absolutePos at the index
            var indexAbsolutePos = AbsolutePos + index * _subBlockSize;

            switch (id[0])
            {
                // if we are reading into a BlockMatrix,
                case 1:
                {
                    // create a new BlockMatrix and add it to the _subBlocks array
                    _subBlocks[index.X, index.Y] = new BlockMatrix<T>(DefaultValue, _subBlockSize, indexAbsolutePos);
                    
                    // and pass the call downwards, into it.
                    _subBlocks[index.X, index.Y].DeserializeBlockMatrix(tree, data);
                    
                    break;
                }
                // otherwise, if we are reading into a BlockMatrixValue,
                case 2:
                {
                    // read the value pointer (uint) from the tree stream
                    var valuePointerBytes = ReadStream(tree, 4, "value pointer");
                    var valuePointer = BitConverter.ToUInt32(valuePointerBytes) * T.SerializeLength;
                    
                    // use that pointer to get the actual value
                    var valueBytes = ReadStream(data, T.SerializeLength, "value", valuePointer);
                    var value = T.Deserialize(valueBytes);
                    
                    // and create a new BlockMatrixValue, with the correct parameters, and add it to the _subBlocks array
                    _subBlocks[index.X, index.Y] = new BlockMatrixValue<T>(DefaultValue, indexAbsolutePos, _subBlockSize, value);
                    
                    break;
                }
            }
            
        }
        
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
    
    internal override void SerializeBlockMatrix(Stream tree, Stream data, Vec2Long index)
    {
        // if the value is default, we don't need to include it in the serialization
        if (_value == DefaultValue)
            return;
        
        // write the identifier for a BlockMatrixValue to the tree stream
        tree.Write(new byte[] { 2 });
        
        // write the default data for a BlockMatrixBlock to the tree stream
        base.SerializeBlockMatrix(tree, data, index);
        
        // write, and get the pointer of, the value in the data stream
        var pointer = (uint)data.Position / T.SerializeLength;
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

    /// <summary>
    /// Reads a stream, advancing the position in the stream by the number of bytes read
    /// </summary>
    /// <param name="stream">the stream to read from</param>
    /// <param name="length">the amount of bytes to read</param>
    /// <param name="name">optional, the name of the object being read</param>
    /// <param name="position">optional, the position within the stream to read from</param>
    /// <returns>a span containing the read bytes</returns>
    internal static ReadOnlySpan<byte> ReadStream(Stream stream, long length, string? name = null, long? position = null)
    {
        // change read position if supplied
        if (position != null)
            stream.Position = (long)position;

        // store the starting position for use in case of an error
        var startingPosition = stream.Position;
        
        // create the output span
        var bytes = new Span<byte>(new byte[length]);
        
        // read from the stream
        var count = stream.Read(bytes);
        
        // error if the stream if the amount of bytes read does not equal the amount of bytes to read in total
        if (count != length)
            Util.Error($"Failed to correctly read {name ?? "stream"}" +
                       $" at position {startingPosition}..{startingPosition + length}" +
                       $" ({count}/{length} bytes read)");
        
        // return the result
        return bytes;
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
