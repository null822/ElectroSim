#nullable enable
using System;

namespace ElectroSim.Maths.BlockMatrix;

public interface IBlockMatrixElement<in T> where T : IBlockMatrixElement<T>
{
    
    public static abstract bool operator ==(T? a, T? b);
    public static abstract bool operator !=(T? a, T? b);
    
    public abstract ReadOnlySpan<byte> Serialize();
    public static abstract long SerializeLength { get; }
}