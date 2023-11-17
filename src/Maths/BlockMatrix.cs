#nullable enable
using System;
using Microsoft.Xna.Framework;

namespace ElectroSim.Maths.BlockMatrix;

// public class BlockMatrix<T>
// {
//     private readonly int _width;
//     private readonly int _height;
//
//     private readonly BlockMatrixPart<T>[,] _contents;
//
//     public BlockMatrix(int width, int height)
//     {
//         _width = width;
//         _height = height;
//
//         var wLargestFactor = BlockMatrixUtil.LargestFactor(width);
//         var hLargestFactor = BlockMatrixUtil.LargestFactor(height);
//
//         _contents = new BlockMatrixPart<T>[width / wLargestFactor, height / hLargestFactor];
//     }
//
//     public bool Add()
//     {
//         
//     }
//     
// }

internal class BlockMatrixPart<T>
{
    protected readonly Vector2 Pos;
    protected readonly Vector2 Size;
    
    protected bool IsEmpty = false;

    protected BlockMatrixPart(Vector2 pos, Vector2 size)
    {
        Pos = pos;
        Size = size;
    }

    public virtual bool Add(Vector2 pos, T value)
    {
        return false;
    }
    
    public virtual bool Remove(Vector2 pos)
    {
        return false;
    }
    
    public virtual T? Get(Vector2 pos)
    {
        return default;
    }

}


internal class BlockMatrix<T> : BlockMatrixPart<T>
{
    private readonly BlockMatrixPart<T>?[,] _contents;

    private readonly Vector2 _partSize;
    
    
    public BlockMatrix(Vector2 size, Vector2 pos = new()) : base(pos, size)
    {
        var wLargestFactor = BlockMatrixUtil.LargestFactor((int)size.X);
        var hLargestFactor = BlockMatrixUtil.LargestFactor((int)size.Y);
        
        // instantiate _contents to be of size: smallest non-1 (unless passed size is prime) factor of passed size.

        _partSize = new Vector2(wLargestFactor, hLargestFactor);
        var contentsSize = new Vector2((int)(size.X / wLargestFactor), (int)(size.X / hLargestFactor));
        _contents = new BlockMatrixPart<T>[(int)contentsSize.X, (int)contentsSize.X];
        
    }


    /// <summary>
    /// Gets a value from the matrix.
    /// </summary>
    /// <param name="pos">the position to get the value from</param>
    /// <returns>the value</returns>
    public override T? Get(Vector2 pos)
    {
        var contentPos = BlockMatrixUtil.GetPartIndexFromPos(pos - Pos, _partSize);
        
        var part = _contents[(int)contentPos.X, (int)contentPos.Y];

        return part == null ? default : part.Get(pos);
    }
    
    public override bool Add(Vector2 pos, T value)
    {
        
        var contentPos = BlockMatrixUtil.GetPartIndexFromPos(pos - Pos, _partSize);
        
        var part = _contents[(int)contentPos.X, (int)contentPos.Y];
        
        // if part is null, create it
        if (part == null)
        {
            part = 
                _partSize == Vector2.One ?
                    new BlockMatrixValue<T>(pos, Vector2.One, value) :
                    new BlockMatrix<T>(_partSize, pos);

            _contents[(int)contentPos.X, (int)contentPos.Y] = part;
        }
        
        
        var success = part.Add(pos, value);

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
        
        var contentPos = BlockMatrixUtil.GetPartIndexFromPos(pos - Pos, _partSize);

        var part = _contents[(int)contentPos.X, (int)contentPos.Y];

        // if part does not exist to begin with, nothing will change
        if (part == null) return false;
        
        
        // if part refers to a BlockMatrixContainer, pass the call downwards.
        if (part.GetType() == typeof(BlockMatrix<T>))
        {
            success = part.Remove(pos);
        }
        
        // otherwise, if pos refers to a BlockMatrixValue, remove it and flatten the matrix.
        else if (part.GetType() == typeof(BlockMatrixValue<T>))
        {
            //TODO: compression
            
            success = true;
        }

        // update the isEmpty flag if something changed
        if (success)
            IsEmpty = CheckEmpty();
        
        return success;
    }


    private bool CheckEmpty()
    {
        var isEmpty = true;
        
        foreach (var part in _contents)
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

internal class BlockMatrixValue<T> : BlockMatrixPart<T>
{
    private T _value;
    
    public BlockMatrixValue(Vector2 pos, Vector2 size, T value) : base(pos, size)
    {
        _value = value;
    }

    public override bool Add(Vector2 pos, T value)
    {
        _value = value;
        return true;
    }

    public override T Get(Vector2 pos)
    {
        return _value;
    }
}

internal static class BlockMatrixUtil
{
    internal static Vector2 GetPartIndexFromPos(Vector2 localPos, Vector2 partSize)
    {
        var contentPos = new Vector2(
            (int)Math.Floor(localPos.X / partSize.X),
            (int)Math.Floor(localPos.X / partSize.X)
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
