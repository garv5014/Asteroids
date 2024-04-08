namespace Asteroids.Shared.GameEntities;

public interface IGameObject
{
  public int Id { get; set; }
  public int XCoordinate { get; set; }
  public int YCoordinate { get; set; }
  public int Rotation { get; set; }
}