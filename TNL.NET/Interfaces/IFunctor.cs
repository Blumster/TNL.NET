namespace TNL.Interfaces;

using TNL.Utils;

public interface IFunctor
{
    void Set(object[] parameters);
    void Read(BitStream stream);
    void Write(BitStream stream);
    void Dispatch(object obj);
}
