namespace PacketHandling.IO;

public ref struct PacketIntermediateProcessing<T>
{
    private readonly T _value;
    private readonly string _packetName;
    private readonly string _fieldName;
    private readonly PacketReader _reader;
    
    public T Value => _value;
    
    public PacketIntermediateProcessing(T value, string packetName, string fieldName, PacketReader reader)
    {
        _value = value;
        _packetName = packetName;
        _fieldName = fieldName;
        _reader = reader;
    }
    
    public PacketIntermediateProcessing<TProcessed> Map<TProcessed>(Func<T, TProcessed> mapper)
    {
        return _reader.ProcessingFailed 
            ? new PacketIntermediateProcessing<TProcessed>(default!, _packetName, _fieldName, _reader) 
            : new PacketIntermediateProcessing<TProcessed>(mapper(_value), _packetName, _fieldName, _reader);
    }

    public PacketIntermediateProcessing<T> Ensure(Predicate<T> condition, PacketErrorCode failureCode, string failureDetail)
    {
        if (_reader.ProcessingFailed) return this;

        if(!condition(_value))
        {
            _reader.Result = PacketResult.Err(failureCode, _packetName, _fieldName, _reader.Consumed, failureDetail);
        }
        
        return this;
    }

    public static implicit operator T(PacketIntermediateProcessing<T> p) => p._value; 
}