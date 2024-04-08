namespace Asteroids.Shared.GameEntities;

public class Asteroid(int id, int xCoordinate, int yCoordinate, int rotation, int size)
  : IGameObject
{
  public int Id { get; set; } = id;
  public int XCoordinate { get; set; } = xCoordinate;
  public int YCoordinate { get; set; } = yCoordinate;
  public int Rotation { get; set; } = rotation;
  public int Size { get; set; } = size;
}