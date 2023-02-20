namespace VRAtlas.Core.Models;

public interface IScopedEventListener<T>
{
    Task Handle(T message);
}